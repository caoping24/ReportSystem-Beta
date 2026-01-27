import myAxios from "@/request";

/**
 * 用户注册
 * @param params
 */
export const userRegister = async (params: any) => {
  return myAxios.request({
    url: "api/user/register",
    method: "POST",
    data: params,
  });
};

/**
 * 用户登录
 * @param params
 */
export const userLogin = async (params: any) => {
  return myAxios.request({
    url: "api/user/login",
    method: "POST",
    data: params,
  });
};

/**
 * 用户注销
 * @param params
 */
export const userLogout = async (params: any) => {
  return myAxios.request({
    url: "api/user/logout",
    method: "POST",
    data: params,
  });
};

/**
 * 获取当前用户
 */
export const getCurrentUser = async () => {
  return myAxios.request({
    url: "api/user/current",
    method: "GET",
  });
};

/**
 * 获取用户列表
 * @param userName
 */
export const searchUsers = async (userName: any) => {
  return myAxios.request({
    url: "api/user/search",
    method: "GET",
    params: {
      userName,
    },
  });
};

/**
 * 删除用户
 * @param id
 */
export const deleteUser = async (id: string) => {
  return myAxios.request({
    url: "api/user/delete",
    method: "POST",
    data: id,
    headers: {
      "Content-Type": "application/json",
    },
  });
};
// 分页查询报表接口
export const getReportByPage = async (params: {
  pageIndex: number;
  pageSize: number;
  type: number;
}) => {
  return myAxios.request({
    url: "/api/ReportRecord/GetReportByPage", // 对应后端接口地址
    method: "GET",
    params: params
  });
};


// 你的下载接口封装文件 【精准修复版】
export const downloadReport = async (timeStr: String, tabKey: number) => { // ✅ 增加 tabKey 参数
  return myAxios.request({
    url: "api/Report/DownloadExcel",
    method: "GET",
    params: { timeStr: timeStr, Type: tabKey }, // ✅ 核心：传2个参数，后端才能正常接收
    responseType: 'blob', 
    timeout: 60000, 
  });
};
// 新增批量下载报表ZIP接口
export const batchDownloadReportZip = async (params: {
  type: number;    // 报表类型 1-日报 2-周报 3-月报 4-年报
  startTime: string; // 开始时间（格式：YYYY/YYYY-MM/YYYY-MM-DD）
  endTime: string;   // 结束时间（格式同上）
}) => {
  return myAxios.request({
    url: "/api/File/ZipFileBigTest", // 后端批量下载接口地址（需和后端确认）
    method: "POST", // 从GET改为POST
    data: params, // GET用params，POST改用data传递请求体参数
    responseType: 'blob', // 必须指定blob，处理zip二进制流
    timeout: 120000, // 批量下载可能耗时更长，设置2分钟超时
  });
};
