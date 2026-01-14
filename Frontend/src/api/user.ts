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
/**
 * 获取报表列表
 * @param id
 */
export const searchReports = async (type: any) => {
  return myAxios.request({
    url: "/api/Report/search",
    method: "GET",
    params: {
      type,
    },
  });
};
// 你的下载接口封装文件 【精准修复版】
export const downloadReport = async (id: number, tabKey: number) => { // ✅ 增加 tabKey 参数
  return myAxios.request({
    url: "api/Report/ExportExcel",
    method: "GET",
    params: { id: id, tabKey: tabKey }, // ✅ 核心：传2个参数，后端才能正常接收
    responseType: 'blob', 
    timeout: 60000, 
  });
};