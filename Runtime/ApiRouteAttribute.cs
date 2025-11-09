using System;

namespace LocalRestAPI.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApiRouteAttribute : Attribute


    {
        public string Method { get; set; }


        public string Path { get; set; }


        public ApiRouteAttribute(string method, string path)


        {
            Method = method.ToUpper();


            Path = path;
        }
    }


// GET请求属性


    [AttributeUsage(AttributeTargets.Method)]
    public class GetRouteAttribute : ApiRouteAttribute


    {
        public GetRouteAttribute(string path) : base("GET", path)

        {
        }
    }


// POST请求属性


    [AttributeUsage(AttributeTargets.Method)]
    public class PostRouteAttribute : ApiRouteAttribute


    {
        public PostRouteAttribute(string path) : base("POST", path)

        {
        }
    }
}