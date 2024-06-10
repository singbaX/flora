namespace Flora.Services
{
    using AiCodo;
    using AiCodo.Data;
    using System;

    public class LangTextService
    {
        static object _ConnectionLock = new object();
        public static string GetText(string key, string defaultText, string langCode)
        {
            lock (_ConnectionLock)
            {
                var item = SqlService.ExecuteScalar("sys_lang_text.GetContent", "TextKey", key, "LangCode", langCode);
                if (item == null || item == DBNull.Value)
                {
                    if (defaultText.IsNullOrEmpty())
                    {
                        return "";
                    }

                    var now = DateTime.Now;
                    SqlService.ExecuteScalar("sys_lang_text.Insert",
                        "TextKey", key, "LangCode", langCode,
                        "Content", defaultText, "TextStyle", "",
                        "IsValid", 1, "CreateUser", 0, "CreateTime", now, "UpdateUser", 0, "UpdateTime", now);
                    return defaultText;
                }
                else
                {
                    return item.ToString();
                }
            }
        }
    }
}
