using Newtonsoft.Json;

namespace B_Torq.Utilities;

public static class Extensions
{ 
    public static T FromJson<T>(this string str)
    {
       return JsonConvert.DeserializeObject<T>(str);
    }

    public static string ToJson<T>(this T obj, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(obj, formatting);
    } 
}