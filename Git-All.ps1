$ErrorActionPreference = 'Stop'
$RepositoryRoot = $PSScriptRoot

function Invoke-GitCommand([string]$RepositoryPath, [string[]]$Arguments) {
    & git -C $RepositoryPath @Arguments

    if ($LASTEXITCODE -ne 0) {
        $ArgumentText = $Arguments -join ' '
        throw "Git command failed in '$RepositoryPath': git $ArgumentText"
    }
}

function Get-SubmodulePaths() {
    $SubmodulePaths = & git -C $RepositoryRoot submodule foreach --recursive --quiet 'echo $sm_path'

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate submodules.'
    }

    return @($SubmodulePaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Enable-GitLongPaths([string]$RepositoryPath) {
    Invoke-GitCommand $RepositoryPath @('config', 'core.longpaths', 'true') | Out-Null
}

function Switch-SubmoduleToMainBranch([string]$RepositoryPath, [string]$RepositoryName) {
    Enable-GitLongPaths $RepositoryPath
    $BranchName = & git -C $RepositoryPath branch --show-current

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read the current branch for '$RepositoryName'."
    }

    if (-not [string]::IsNullOrWhiteSpace($BranchName)) {
        return $BranchName
    }

    Write-Host "[$RepositoryName] Detached HEAD; switching to main..." -ForegroundColor Yellow
    & git -C $RepositoryPath show-ref --verify --quiet refs/heads/main
    $HasLocalMainBranch = $LASTEXITCODE -eq 0

    if ($HasLocalMainBranch) {
        Invoke-GitCommand $RepositoryPath @('switch', 'main') | Out-Host
    }
    else {
        Invoke-GitCommand $RepositoryPath @('switch', '--track', 'origin/main') | Out-Host
    }

    return 'main'
}

function Get-CommitMessage() {
    $CommitMessage = Read-Host 'Commit message'

    if ([string]::IsNullOrWhiteSpace($CommitMessage)) {
        throw 'A commit message is required.'
    }

    return $CommitMessage
}

function Commit-And-PushRepository([string]$RepositoryPath, [string]$RepositoryName, [string]$CommitMessage) {
    Write-Host "`n[$RepositoryName] Staging all changes..." -ForegroundColor Cyan
    Invoke-GitCommand $RepositoryPath @('add', '-A')

    & git -C $RepositoryPath diff --cached --quiet
    $HasStagedChanges = $LASTEXITCODE -ne 0

    if ($HasStagedChanges) {
        Write-Host "[$RepositoryName] Committing..." -ForegroundColor Cyan
        Invoke-GitCommand $RepositoryPath @('commit', '-m', $CommitMessage)
    }
    else {
        Write-Host "[$RepositoryName] No changes to commit." -ForegroundColor DarkGray
    }

    Write-Host "[$RepositoryName] Pushing..." -ForegroundColor Cyan
    Invoke-GitCommand $RepositoryPath @('push')
}

function Commit-And-PushAll() {
    $CommitMessage = Get-CommitMessage
    $SubmodulePaths = Get-SubmodulePaths

    Enable-GitLongPaths $RepositoryRoot

    foreach ($SubmodulePath in $SubmodulePaths) {
        $SubmoduleRepositoryPath = Join-Path $RepositoryRoot $SubmodulePath
        Switch-SubmoduleToMainBranch $SubmoduleRepositoryPath $SubmodulePath | Out-Null
        Commit-And-PushRepository $SubmoduleRepositoryPath $SubmodulePath $CommitMessage
    }

    Commit-And-PushRepository $RepositoryRoot 'Root repository' $CommitMessage
}

function Pull-All() {
    Enable-GitLongPaths $RepositoryRoot
    Write-Host "`n[Root repository] Pulling..." -ForegroundColor Cyan
    Invoke-GitCommand $RepositoryRoot @('pull', '--recurse-submodules')
    Invoke-GitCommand $RepositoryRoot @('submodule', 'sync', '--recursive')
    Invoke-GitCommand $RepositoryRoot @('submodule', 'update', '--init', '--recursive')

    $SubmodulePaths = Get-SubmodulePaths

    foreach ($SubmodulePath in $SubmodulePaths) {
        $SubmoduleRepositoryPath = Join-Path $RepositoryRoot $SubmodulePath
        $BranchName = Switch-SubmoduleToMainBranch $SubmoduleRepositoryPath $SubmodulePath

        Write-Host "[$SubmodulePath] Pulling branch '$BranchName'..." -ForegroundColor Cyan
        Invoke-GitCommand $SubmoduleRepositoryPath @('pull')
    }
}

function Show-Menu([int]$Selection) {
    Clear-Host
    Write-Host 'Git all repositories' -ForegroundColor Cyan
    Write-Host 'Use Up/Down arrows and Enter. Esc exits.'
    Write-Host ''

    $Options = @(
        'Commit and push all, including submodules',
        'Pull all, including submodules'
    )

    for ($Index = 0; $Index -lt $Options.Count; $Index++) {
        if ($Index -eq $Selection) {
            Write-Host "> $($Options[$Index])" -ForegroundColor Black -BackgroundColor Cyan
        }
        else {
            Write-Host "  $($Options[$Index])"
        }
    }
}

function Select-Action() {
    $Selection = 0

    while ($true) {
        Show-Menu $Selection
        $Key = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown').VirtualKeyCode

        switch ($Key) {
            38 {
                $Selection = ($Selection - 1 + 2) % 2
                break
            }
            40 {
                $Selection = ($Selection + 1) % 2
                break
            }
            13 {
                return $Selection
            }
            27 {
                return $null
            }
        }
    }
}

try {
    $Action = Select-Action

    if ($null -eq $Action) {
        exit 0
    }

    if ($Action -eq 0) {
        Commit-And-PushAll
    }
    else {
        Pull-All
    }

    Write-Host "`nCompleted successfully." -ForegroundColor Green
}
catch {
    Write-Host "`nFailed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    Read-Host "`nPress Enter to close"
}
