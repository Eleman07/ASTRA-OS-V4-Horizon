; -*- coding: utf-8 -*-
Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "x64.nsh"
!include "WinVer.nsh"
!include "${GENERATED_PAYLOAD}"
!ifndef PRODUCT_VERSION
!define PRODUCT_VERSION "4.2.0.0"
!endif

Name "ASTRA OS V4 Horizon Cloud"
OutFile "${OUTPUT_EXE}"
InstallDir "$LOCALAPPDATA\Programs\ASTRA OS Horizon Cloud"
RequestExecutionLevel user
SetCompressor zlib
CRCCheck on
SetOverwrite on
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=1033 "ProductName" "ASTRA OS V4 Horizon Cloud"
VIAddVersionKey /LANG=1033 "FileDescription" "ASTRA OS Horizon Cloud installer"
VIAddVersionKey /LANG=1033 "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=1033 "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=1033 "LegalCopyright" "Emanuele Magno and ASTRA contributors"
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "ASTRA OS V4 Horizon Cloud"
!define MUI_WELCOMEPAGE_TEXT "Install ASTRA Assistant with online specialist agents, an Obsidian knowledge base and an interactive knowledge graph. OpenRouter internet access and an API key are required. Free-provider quotas apply. No local model weights are included.$\r$\n$\r$\nClose ASTRA and disable its background worker before updating an existing installation. Notes, PDFs, chats and preferences stay in their existing per-account location.$\r$\n$\r$\nMicrosoft prerequisites are included and can be installed separately if needed."
!define MUI_FINISHPAGE_RUN "$INSTDIR\Astra.Desktop.exe"
!define MUI_FINISHPAGE_RUN_NOTCHECKED
!define MUI_FINISHPAGE_RUN_TEXT "Start ASTRA OS Horizon Cloud"
!define MUI_FINISHPAGE_TEXT "ASTRA is installed. Open it from the Start menu.$\r$\n$\r$\nIf a Microsoft runtime is missing, open the Prerequisites Start menu entry. See README for full screen and Windows taskbar auto-hide."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${APP_LICENSE}"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Italian"

Var QaToken
Var ProductKey
Var MenuName
Var DesktopName

!macro DebugInit Text
!ifdef DEBUG_LOG
    FileOpen $9 "${DEBUG_LOG}" a
    FileSeek $9 0 END
    FileWrite $9 "${Text}$\r$\n"
    FileClose $9
!endif
!macroend

!macro ConfigureIdentity Prefix
Function ${Prefix}ConfigureIdentity
    !insertmacro DebugInit "Configure identity: $QaToken"
    StrCpy $ProductKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\ASTRAOSHorizonCloud"
    StrCpy $MenuName "ASTRA OS Horizon Cloud"
    StrCpy $DesktopName "ASTRA OS Horizon Cloud.lnk"
    ${If} $QaToken != ""
        StrLen $0 $QaToken
        System::Call 'shlwapi::StrSpnW(w "$QaToken", w "0123456789abcdef") i.r1'
        !insertmacro DebugInit "Token length $0, hex prefix $1"
        ${If} $0 != 32
        ${OrIf} $1 != 32
            Abort "Invalid isolated test identity."
        ${EndIf}
        StrCpy $ProductKey "$ProductKey-QA-$QaToken"
        StrCpy $MenuName "ASTRA OS Horizon Cloud QA-$QaToken"
        StrCpy $DesktopName "ASTRA OS Horizon Cloud QA-$QaToken.lnk"
    ${EndIf}
FunctionEnd

Function ${Prefix}ValidateDirectory
    !insertmacro DebugInit "Validate directory: $INSTDIR"
    ; Win32 canonicalization also supports a destination not yet created.
    ; NSIS GetFullPathName returns an empty string for a missing parent folder.
    System::Call 'kernel32::GetFullPathNameW(w "$INSTDIR", i ${NSIS_MAX_STRLEN}, w .r2, p 0) i.r3'
    ${If} $3 == 0
    ${OrIf} $3 >= ${NSIS_MAX_STRLEN}
        Abort "The installation path is invalid or too long."
    ${EndIf}
    StrCpy $INSTDIR "$2"
    ${GetRoot} "$INSTDIR" $0
    !insertmacro DebugInit "Root: $0"
    ${If} $INSTDIR == $0
    ${OrIf} $INSTDIR == "$0\"
        Abort "Choose an application folder, not a drive root."
    ${EndIf}
    StrCpy $0 "$INSTDIR"
    ${Do}
        System::Call 'kernel32::GetFileAttributesW(w r0) i.r1'
        !insertmacro DebugInit "Attributes: $0 = $1"
        ${If} $1 != -1
            IntOp $1 $1 & 0x400
            ${If} $1 != 0
                Abort "The application folder must not use symbolic links or junctions."
            ${EndIf}
        ${EndIf}
        ${GetParent} "$0" $1
        !insertmacro DebugInit "Parent: $1"
        ${If} $1 == ""
        ${OrIf} $1 == $0
            ${ExitDo}
        ${EndIf}
        StrCpy $0 "$1"
    ${Loop}
FunctionEnd
!macroend
!insertmacro ConfigureIdentity ""
!insertmacro ConfigureIdentity "un."

Function .onInit
    !insertmacro DebugInit "Init: $INSTDIR"
    ${IfNot} ${RunningX64}
        MessageBox MB_OK|MB_ICONSTOP "ASTRA requires Windows 11 x64."
        Abort
    ${EndIf}
    System::Call 'kernel32::GetCurrentProcess() p.r0'
    System::Call 'kernel32::IsWow64Process2(p r0, *i .r1, *i .r2) i.r3'
    IntOp $2 $2 & 0xFFFF
    !insertmacro DebugInit "Architecture: result $3, process $1, native $2"
    ${If} $3 == 0
    ${OrIf} $2 != 34404
        MessageBox MB_OK|MB_ICONSTOP "This installer requires an x64 PC. ARM64 is not supported by this package."
        Abort
    ${EndIf}
    ${IfNot} ${AtLeastWin10}
        MessageBox MB_OK|MB_ICONSTOP "ASTRA requires Windows 11."
        Abort
    ${EndIf}
    SetRegView 64
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "CurrentBuildNumber"
    !insertmacro DebugInit "Windows build: $0"
    ${If} $0 < 22000
        MessageBox MB_OK|MB_ICONSTOP "ASTRA requires Windows 11 build 22000 or later."
        Abort
    ${EndIf}
    SetShellVarContext current
    ${GetParameters} $0
    ${GetOptions} $0 "/ASTRA-QA=" $QaToken
    Call ConfigureIdentity
FunctionEnd

Section "ASTRA OS Horizon Cloud (required)" SecApp
    SectionIn RO
    Call ValidateDirectory
    IfFileExists "$INSTDIR\.astra-installation.ini" ExistingFolder CheckEmpty
    ExistingFolder:
        ReadINIStr $0 "$INSTDIR\.astra-installation.ini" "ASTRA" "Application"
        ReadINIStr $1 "$INSTDIR\.astra-installation.ini" "ASTRA" "QA"
        ${If} $0 != "ASTRA.OS.Horizon.Cloud"
        ${OrIf} $1 != $QaToken
            Abort "This folder belongs to a different installation."
        ${EndIf}
        Goto CopyPayload
    CheckEmpty:
        FindFirst $0 $1 "$INSTDIR\*"
        !insertmacro DebugInit "First directory entry: $1"
        ${DoWhile} $1 != ""
            ${If} $1 != "."
            ${AndIf} $1 != ".."
                FindClose $0
                Abort "Choose an empty folder or an existing ASTRA installer destination."
            ${EndIf}
            FindNext $0 $1
        ${Loop}
        FindClose $0
    CopyPayload:
    !insertmacro ASTRA_INSTALL_PAYLOAD
    SetOutPath "$INSTDIR"
    WriteINIStr "$INSTDIR\.astra-installation.ini" "ASTRA" "Application" "ASTRA.OS.Horizon.Cloud"
    WriteINIStr "$INSTDIR\.astra-installation.ini" "ASTRA" "QA" "$QaToken"
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    CreateDirectory "$SMPROGRAMS\$MenuName"
    CreateShortCut "$SMPROGRAMS\$MenuName\ASTRA OS Horizon Cloud.lnk" "$INSTDIR\Astra.Desktop.exe" "" "$INSTDIR\Assets\astra.ico"
    CreateShortCut "$SMPROGRAMS\$MenuName\Prerequisites.lnk" "$INSTDIR\prerequisites"
    CreateShortCut "$SMPROGRAMS\$MenuName\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "$ProductKey" "DisplayName" "ASTRA OS V4 Horizon Cloud"
    WriteRegStr HKCU "$ProductKey" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKCU "$ProductKey" "Publisher" "Emanuele Magno"
    WriteRegStr HKCU "$ProductKey" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "$ProductKey" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
    WriteRegStr HKCU "$ProductKey" "DisplayIcon" "$INSTDIR\Assets\astra.ico"
    WriteRegDWORD HKCU "$ProductKey" "NoModify" 1
    WriteRegDWORD HKCU "$ProductKey" "NoRepair" 1
    WriteRegDWORD HKCU "$ProductKey" "EstimatedSize" ${INSTALLED_KB}
SectionEnd

Section /o "Desktop shortcut" SecDesktop
    CreateShortCut "$DESKTOP\$DesktopName" "$INSTDIR\Astra.Desktop.exe" "" "$INSTDIR\Assets\astra.ico"
SectionEnd

Function un.onInit
    SetShellVarContext current
    SetRegView 64
    Call un.ValidateDirectory
    ReadINIStr $0 "$INSTDIR\.astra-installation.ini" "ASTRA" "Application"
    ${If} $0 != "ASTRA.OS.Horizon.Cloud"
        Abort "ASTRA installation marker is missing."
    ${EndIf}
    ClearErrors
    ReadINIStr $QaToken "$INSTDIR\.astra-installation.ini" "ASTRA" "QA"
    ${If} ${Errors}
        Abort "ASTRA installation marker is incomplete."
    ${EndIf}
    Call un.ConfigureIdentity
FunctionEnd

Section "Uninstall"
    ; Each compiled file path is removed explicitly. Never recursively delete a
    ; chosen directory or the user's separate notes/PDF/chat/preferences store.
    !insertmacro ASTRA_UNINSTALL_PAYLOAD
    Delete "$SMPROGRAMS\$MenuName\ASTRA OS Horizon Cloud.lnk"
    Delete "$SMPROGRAMS\$MenuName\Prerequisites.lnk"
    Delete "$SMPROGRAMS\$MenuName\Uninstall.lnk"
    RMDir "$SMPROGRAMS\$MenuName"
    Delete "$DESKTOP\$DesktopName"
    DeleteRegKey HKCU "$ProductKey"
    Delete "$INSTDIR\.astra-installation.ini"
    Delete "$INSTDIR\Uninstall.exe"
    SetOutPath "$TEMP"
    RMDir "$INSTDIR"
    Goto UninstallDone
    UninstallFailed:
        SetErrorLevel 1
        MessageBox MB_OK|MB_ICONEXCLAMATION "Some application files are in use. Close ASTRA and run Uninstall again." /SD IDOK
    UninstallDone:
SectionEnd
