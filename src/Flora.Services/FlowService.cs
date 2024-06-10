using AiCodo;
using AiCodo.Data;
using AiCodo.Flow;
using AiCodo.Flow.Configs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flora.Services
{
    public class FlowService
    {
        public static Task<ServiceResult> Execute(string serviceName, Dictionary<string, object> args)
        {
            ServiceItemBase item;
            if (!ServiceIndex.Current.TryGetItem(serviceName, out item))
            {
                return Task.FromResult(new ServiceResult
                {
                    ErrorCode = FlowErrors.ServiceNotFound,
                    Error = ErrorCodes.Current.GetErrorMessage(FlowErrors.ServiceNotFound, serviceName)
                });
            }

            if (item.PageID.IsNotEmpty() && item.AuthValue > 0)
            {
                if (ServiceLocator.Current != null && ServiceLocator.Current.TryGet<IUserService>(out var service))
                {
                    return Task.FromResult(new ServiceResult
                    {
                        ErrorCode = FlowErrors.ServiceDenied,
                        Error = ErrorCodes.Current.GetErrorMessage(FlowErrors.ServiceDenied, serviceName)
                    });
                }
            }

            if (item.ServiceArgs.IsNotEmpty())
            {
                var nv = item.ServiceArgs.ToUrlParameters();
                if (nv != null)
                {
                    foreach (var a in nv)
                    {
                        if (args.ContainsKey(a.Key))
                        { 
                            //请求参数中的值会被替换
                            "FlowService".Log($"服务[{serviceName}]参数[{a.Key}]被固定参数替换", Category.Warn);
                        }
                        args[a.Key] = a.Value;
                    }
                }
            }

            return ServiceFactory.RunItem(item, args);
        }
    }
}
