// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。

namespace Flora.Services
{
    using AiCodo;
    using AiCodo.Data;
    using System;
    using System.Linq;

    public class UserService : IUserService
    {
        const string Sql_GetAuthValue = "sys_user.GetAuthValue";

        public UserService()
        {

        }

        public IUser Login(string username, string password)
        {
            var user = SqlService.ExecuteQuery<DynamicEntity>("sys_user.SelectByUserName", "UserName", username).FirstOrDefault();
            if (user == null)
            {
                if (username.Equals("admin"))
                {
                    var count = SqlService.ExecuteScalar("sys_user.Count").ToInt32();
                    if (count == 0)
                    {
                        user = new DynamicEntity(
                            "UserName", username,
                            "DeptID", 0,
                            "Password", "123456".EncryptMd5_32(),
                            "FullName", "管理员",
                            "Phone", "",
                            "Email", "",
                            "IsValid", 1,
                            "CreateUser", 0,
                            "CreateTime", DateTime.Now,
                            "UpdateUser", 0,
                            "UpdateTime", DateTime.Now);
                        var id = SqlService.ExecuteScalar("sys_user.Insert", user.ToNameValues());
                        return new UserModel
                        {
                            UserID = id.ToString(),
                            UserName = username,
                        };
                    }
                }

                this.Log($"login user:{username},not exists");
                return null;
            }
            var pwd = user.GetString("Password");
            if (!pwd.Equals(password.EncryptMd5_32()))
            {
                this.Log($"login user:{username},password error");
                return null;
            }

            this.Log($"login user:{username}");
            return new UserModel
            {
                UserID = user.GetString("ID"),
                UserName = username,
            };
        }

        //TODO：可以通过缓存改善性能
        public bool CanAccess(string userID, string pageID, int authValue)
        {
            var items = SqlService.ExecuteQuery<DynamicEntity>(Sql_GetAuthValue, "UserID", userID, "PageID", pageID).ToList();
            if (items.Count == 0)
            {
                return false;
            }
            var v = 0;
            foreach (var item in items)
            {
                v = v | item.GetInt32("AuthValue");
            }
            return (v & authValue) == authValue;
        }
    }

    public class UserModel : Entity, IUser
    {
        public string UserName { get; set; }
        public string UserID { get; set; }
    }
}
