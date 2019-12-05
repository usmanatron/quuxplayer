# DotNET and MSI version checking macro.
# Written by AnarkiNet(AnarkiNet@gmail.com) originally, modified by eyal0 (for use in http://www.sourceforge.net/projects/itwister)
# MSI check code based on http://www.codeproject.com/useritems/NSIS.asp
# Downloads the MSI version 3.1 and runs it if the user does not have the correct version.
# Downloads and runs the Microsoft .NET Framework version 2.0 Redistributable and runs it if the user does not have the correct version.
# To use, call the macro with a string:
# Example: non real version numbers
# !insertmacro CheckDotNET "2"
# !insertmacro CheckDotNET "2.0.9"
# (Version 2.0.9 is less than version 2.0.10.)
# Example: latest real version number at time of writing
# !insertmacro CheckDotNET "2.0.50727"
# All register variables are saved and restored by CheckDotNet
# No output
 
!macro CheckDotNET DotNetReqVer
  ;!define DOTNET_URL "http://www.microsoft.com/downloads/info.aspx?na=90&p=&SrcDisplayLang=en&SrcCategoryId=&SrcFamilyId=0856eacb-4362-4b0d-8edd-aab15c5e04f5&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f5%2f6%2f7%2f567758a3-759e-473e-bf8f-52154438565a%2fdotnetfx.exe"
  
  !define DOTNET_URL "http://download.microsoft.com/download/0/6/1/061F001C-8752-4600-A198-53214C69B51F/dotnetfx35setup.exe"
  
  !define MSI31_URL "http://download.microsoft.com/download/1/4/7/147ded26-931c-4daf-9095-ec7baf996f46/WindowsInstaller-KB893803-v2-x86.exe"
 
  DetailPrint "Checking your .NET Framework version..."
  ;callee register save
  Push $0
  Push $1
  Push $2
  Push $3
  Push $4
  Push $5
  Push $6 ;backup of intsalled ver
  Push $7 ;backup of DoNetReqVer
 
 ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
 ;                               MSI                                          ;
 ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  GetDLLVersion "$SYSDIR\msi.dll" $R0 $R1
  IntOp $R2 $R0 / 0x00010000 ; $R2 now contains major version
  IntOp $R3 $R0 & 0x0000FFFF ; $R3 now contains minor version
  IntOp $R4 $R1 / 0x00010000 ; $R4 now contains release
  IntOp $R5 $R1 & 0x0000FFFF ; $R5 now contains build
  StrCpy $0 "$R2.$R3.$R4.$R5" ; $0 now contains string like "1.2.0.192"
 
  ${If} $R2 > '3'
    goto NewMSI
  ${EndIf}
  
  ${If} $R2 < '3'
    goto NeedMSI
  ${EndIf}
  
  ; $R2 == 3
  
  ${If} $R3 > '0'
    goto NewMSI
  ${EndIf}
  
NeedMSI:
    
    SetOutPath "$TEMP"
    SetOverwrite on
 
    MessageBox MB_YESNOCANCEL|MB_ICONEXCLAMATION \
    "Your MSI version: $0.$\nRequired Version: 3.1 or greater.$\nDownload MSI version from www.microsoft.com?" \
    /SD IDYES IDYES DownloadMSI IDNO NewMSI
    goto GiveUpDotNET ;IDCANCEL
  
DownloadMSI:

  DetailPrint "Beginning download of MSI3.1."
  NSISDL::download ${MSI31_URL} "$TEMP\WindowsInstaller-KB893803-v2-x86.exe"
  DetailPrint "Completed download."
  Pop $0
  ${If} $0 == "cancel"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download cancelled.  Continue Installation?" \
    IDYES NewMSI IDNO GiveUpDotNET
  ${ElseIf} $0 != "success"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download failed:$\n$0$\n$\nContinue Installation?" \
    IDYES NewMSI IDNO GiveUpDotNET
  ${EndIf}
  DetailPrint "Pausing installation while downloaded MSI3.1 installer runs."
  ExecWait '$TEMP\WindowsInstaller-KB893803-v2-x86.exe /quiet /norestart' $0
  DetailPrint "Completed MSI3.1 install/update. Exit code = '$0'. Removing MSI3.1 installer."
  Delete "$TEMP\WindowsInstaller-KB893803-v2-x86.exe"
  DetailPrint "MSI3.1 installer removed."
  goto NewMSI
 
NewMSI:

  DetailPrint "MSI3.1 installed. Proceeding with remainder of installation."
  
 ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
 ;                                  NetFX                                     ;
 ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
 
 DetailPrint "Microsoft .NET Framework Version ${DOTNET_VERSION} needed."
 MessageBox MB_YESNOCANCEL|MB_ICONEXCLAMATION \
     "The Microsoft .NET Framework version ${DOTNET_VERSION} is needed. Download from www.microsoft.com?" \ 
    /SD IDYES IDYES DownloadDotNET IDNO NewDotNET
    goto GiveUpDotNET ;IDCANCEL 
 
DownloadDotNET:
  DetailPrint "Beginning download of latest .NET Framework version."
  NSISDL::download ${DOTNET_URL} "$TEMP\dotnetfx.exe"
  DetailPrint "Completed download."
  Pop $0
  ${If} $0 == "cancel"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download cancelled.  Continue Installation?" \
    IDYES NewDotNET IDNO GiveUpDotNET
  ${ElseIf} $0 != "success"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download failed:$\n$0$\n$\nContinue Installation?" \
    IDYES NewDotNET IDNO GiveUpDotNET
  ${EndIf}
 
  DetailPrint "Pausing installation while downloaded .NET Framework installer runs."
  ExecWait '$TEMP\dotnetfx.exe';    /q /c:"install /q"'
  DetailPrint "Completed .NET Framework install/update. Removing .NET Framework installer."
  Delete "$TEMP\dotnetfx.exe"
  DetailPrint ".NET Framework installer removed."
  goto NewDotNet
 
GiveUpDotNET:
  Abort "Installation cancelled by user."
 
NewDotNET:
  DetailPrint "Proceeding with remainder of installation."
  Pop $7
  Pop $6
  Pop $5
  Pop $4
  Pop $3
  Pop $2
  Pop $1
  Pop $0
!macroend
