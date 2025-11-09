using System;

namespace LocalRestAPI.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApiRouteAttribute : Attribute


    {
        public string Method { get; set; }


        public string Path { get; set; }


        public bool NeedsMainThread { get; set; }


        public ApiRouteAttribute(string method, string path, bool needsMainThread = false)


        {
            Method = method.ToUpper();


            Path = path;

            NeedsMainThread = needsMainThread;
        }
    }


// GET请求属性


    [AttributeUsage(AttributeTargets.Method)]
    public class GetRouteAttribute : ApiRouteAttribute


    {
        public GetRouteAttribute(string path, bool needsMainThread = false) : base("GET", path, needsMainThread)

        {
        }
    }


// POST请求属性


    [AttributeUsage(AttributeTargets.Method)]
    public class PostRouteAttribute : ApiRouteAttribute


    {
        public PostRouteAttribute(string path, bool needsMainThread = false) : base("POST", path, needsMainThread)

        {
        }
    }
}