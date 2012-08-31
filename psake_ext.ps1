function Get-Git-Commit
{
  $gitLog = git log --oneline -1
  return $gitLog.Split(' ')[0]
}

function Generate-Assembly-Info
{
param(
  [string]$company, 
  [string]$copyright, 
  [string]$version,
  [string]$sem_version,
  [string]$file = $(throw "file is a required parameter.")
)
  $commit = Get-Git-Commit
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyInformationalVersionAttribute(""$sem_version / $commit"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
[assembly: AssemblyDelaySignAttribute(false)]
"

  $dir = [System.IO.Path]::GetDirectoryName($file)
  if ([System.IO.Directory]::Exists($dir) -eq $false)
  {
    Write-Host "Creating directory $dir"
    [System.IO.Directory]::CreateDirectory($dir)
  }
  Write-Host "Generating assembly info file: $file"
  Write-Output $asmInfo > $file
}