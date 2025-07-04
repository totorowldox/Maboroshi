namespace Maboroshi.Util;

public static class TimeUtil
{
    public static string CurrentTime => DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    
    public static string CurrentDate => DateTime.Now.ToString("yyyy-MM-dd");
}