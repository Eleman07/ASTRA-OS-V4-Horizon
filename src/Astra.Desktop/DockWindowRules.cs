namespace AstraOS.Desktop;

// Taskbar eligibility is separate from native enumeration so it can be tested
// without inspecting personal window titles or activating unrelated applications.
internal static class DockWindowRules
{
    public static bool Include(int processId,int astraProcessId,long style,long extendedStyle,bool owned,string className)
    {
        const long child=0x40000000,tool=0x80,noActivate=0x08000000,app=0x40000;
        if(processId<=0||processId==astraProcessId||(style&child)!=0)return false;
        if((extendedStyle&(tool|noActivate))!=0)return false;
        if(owned&&(extendedStyle&app)==0)return false;
        return className is not ("Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "MultitaskingViewFrame");
    }
}
