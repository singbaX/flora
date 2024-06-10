// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo;
using AiCodo.Data;
using AiCodo.Flow;
using Flora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flora.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        IUserService _UserService;
        public ServiceController(IUserService userService)
        {
            _UserService = userService;
        }

        #region flow Service
#if DEBUG
        [HttpGet]
        [Route("/service/{module}/{page}/{name}")]
        public Task<IActionResult> Get(string module, string page, string name)
        {
            return ExecuteService(module, page, name);
        }
#endif

        [HttpPost]
        [Route("/Service/{module}/{page}/{name}")]
        public Task<IActionResult> Post(string module, string page, string name)
        {
            return ExecuteService(module, page, name);
        }

        [HttpPost]
        [Route("/api/test")]
        public IActionResult AuthTest()
        {
            return Ok();
        }
        #endregion

        #region ExecuteService
        private Task<IActionResult> ExecuteService(string module, string page, string name)
        {
            string serviceName = $"{module}/{page}/{name}";
            var args = GetRequestParameters(out var currentUserID);

            return FlowService.Execute(serviceName, args)
                .ContinueWith<IActionResult>(t =>
                {
                    if (t.Exception != null)
                    {
                        var error = new ServiceResult
                        {
                            Error = t.Exception.Message
                        };
                        return new ContentResult()
                        {
                            Content = error.ToJson(),
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }

                    var result = t.Result;
                    if (result.Data is IFileResult file)
                    {
                        var fileName = file.FileName;
                        if (fileName.IsNullOrEmpty())
                        {
                            return new NoContentResult();
                        }

                        if (fileName.IsFileNotExists())
                        {
                            return NotFound();
                        }

                        var fileContentType = fileName.EndsWith("json", StringComparison.OrdinalIgnoreCase) ?
                            "application/json" : fileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase) ?
                            "application/xml" : fileName.EndsWith("pdf", StringComparison.OrdinalIgnoreCase) ?
                            "application/pdf" : "application/octet-stream";
                        return new PhysicalFileResult(fileName, fileContentType);
                    }

                    return new ContentResult()
                    {
                        Content = t.Result.ToJson(),
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                });
        }

        private Dictionary<string, object> GetRequestParameters(out string currentUserID)
        {
            var parameters = new Dictionary<string, object>();

            #region 从请求对象获取执行参数 Get parameters from request object
            if (Request != null)
            {
                Request.Query
                    .ForEach(p => parameters[p.Key] = p.Value);

                if (Request.ContentType != null)
                {
                    if (Request.ContentType == "application/x-www-form-urlencoded" || Request.ContentType.IndexOf("multipart/form-data") >= 0)
                    {
                        if (Request.Form != null)
                        {
                            #region form 及上传文件处理
                            Dictionary<string, string> files = new Dictionary<string, string>();
                            if (Request.Form.Files != null)
                            {
                                foreach (var file in Request.Form.Files)
                                {
                                    if (TrySaveFile(file, out var fileName))
                                    {
                                        parameters[file.Name] = fileName;
                                        files.Add(file.Name, fileName);
                                    }
                                    else
                                    {
                                        //先抛异常，以后再处理
                                        throw new Exception("文件上传失败");
                                    }
                                }
                            }
                            foreach (var k in Request.Form.Keys)
                            {
                                if (files.ContainsKey(k))
                                {
                                    continue;
                                }
                                parameters[k] = Request.Form[k];
                            }
                            #endregion
                        }
                    }
                    else if (Request.ContentType.IndexOf("application/json") > -1 && Request.Body != null)
                    {
                        DynamicEntity data = Request.Body.ReadToEnd();
                        if (data != null)
                        {
                            data.ForEach(p => parameters[p.Key] = p.Value);
                        }
                    }
                }
            }
            #endregion

            var claims = HttpContext.User.Claims;
            var userID = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var userName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            currentUserID = userID == null ? "" : userID.Value;
            parameters["CurrentUserID"] = userID == null ? 0 : userID.Value.ToInt32();
            parameters["CurrentUserName"] = userName == null ? "" : userName.Value;
            parameters["CurrentDateTime"] = DateTime.Now;
            return parameters;
        }

        private bool TrySaveFile(IFormFile file, out string fileName)
        {
            //设置文件上传路径
            fileName = $"uploads//{DateTime.Now:yyyyMMdd}//{Guid.NewGuid():n}{Path.GetExtension(file.FileName)}";
            var fullFileName = fileName.FixedAppDataPath();
            var filePath = Path.GetDirectoryName(fullFileName);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
                this.Log($"创建上传文件夹：[{filePath}]");
            }

            try
            {
                //将流写入文件
                using (Stream stream = file.OpenReadStream())
                {
                    // 把 Stream 转换成 byte[]
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    // 设置当前流的位置为流的开始
                    stream.Seek(0, SeekOrigin.Begin);
                    // 把 byte[] 写入文件
                    FileStream fs = new FileStream(fullFileName, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(bytes);
                    bw.Close();
                    fs.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                ex.WriteErrorLog();
                return false;
            }
        }
        #endregion
    }
}
