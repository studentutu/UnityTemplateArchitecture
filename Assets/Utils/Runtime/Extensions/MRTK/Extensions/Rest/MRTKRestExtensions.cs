using Microsoft.MixedReality.Toolkit.Utilities;

public static class MRTKRestExtensions
{
    public static string GetPrettyResponse(this Response res)
    {
        return string.Format("Code : {0}, {1}", res.ResponseCode, res.ResponseBody);
    }

    public static Response CreateBadResponse(this Response res, string message)
    {
        return new Response(false, message, null, 400);
    }
    
    public static Response CreateGoodResponse(this Response res, string message)
    {
        return new Response(true, message, null, 200);
    }
}