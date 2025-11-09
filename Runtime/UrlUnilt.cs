namespace LocalRestAPI.Runtime
{
    public static class UrlUtility
    {
        /// <summary>
        /// 定向到api的路径
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string Combine(string baseUri, string path)
        {
            return baseUri.TrimEnd('/') + "/" + path.TrimStart('/');
        }
        
        ///验证url是否合法
        public static bool IsUrl(string url)
        {
            return url.StartsWith("http://") || url.StartsWith("https://")||url.EndsWith("/");
        }
        
       
    }
}