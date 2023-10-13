using System;
namespace OTM.DevLOG.Extensions;



public static class DateTimeExtensions
{
    public static System.Boolean IsNightTime( this System.DateTime value )
    {
        return value.Hour >= 0 && value.Hour <= 4;
    }

    public static System.Boolean IsEarlyMorning(this System.DateTime value)
    {
        return value.Hour >= 5 && value.Hour <= 6;
    }
}

