namespace HMSX.Second.Plugin.Tool
{
    /// <summary>
    /// 结果
    /// </summary>
    /// <typeparam name="T">Data类型</typeparam>
    /// <typeparam name="V">Ext类型</typeparam>
    public class Result<T, V>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// Message
        /// </summary>
        public string Msg { get; set; } = "";

        /// <summary>
        /// 附加信息
        /// </summary>
        public V Ext { get; set; } = default;

        /// <summary>
        /// 返回数据
        /// </summary>
        public T Data { get; set; } = default;
    }
}
