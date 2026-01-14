import axios, { AxiosResponse } from "axios";
import { message } from "ant-design-vue";

// ========== 核心：扩展AxiosResponse类型，添加自定义的文件下载属性，解决TS类型报错 ==========
declare module 'axios' {
  interface AxiosResponse {
    fileBlobData?: Blob;   // 存放blob文件流
    fileDownloadName?: string; // 存放解析后的文件名
  }
}

const myAxios = axios.create({
  baseURL: process.env.NODE_ENV === 'development' ? "http://localhost:5260" : "http://118.31.13.93:80",
  timeout: 10000,
  withCredentials: true,
});

// 请求拦截器：基础配置 + 请求前处理 【无修改，保留你的原代码】
myAxios.interceptors.request.use(
  function (config) {
    if (!config.headers['Content-Type']) {
      config.headers['Content-Type'] = 'application/json;charset=UTF-8';
    }
    return config;
  },
  function (error) {
    message.error('请求配置异常，请检查参数或网络设置');
    console.error('请求拦截器错误：', error);
    return Promise.reject(error);
  }
);

// 响应拦截器：完整逻辑 + blob文件下载处理 + TS类型完美兼容 + 无任何报错
myAxios.interceptors.response.use(
  function (response: AxiosResponse): AxiosResponse {
    // ========== ✅ 1. Excel导出 blob文件流 核心处理 - 优先级最高 ==========
    if (response.config.responseType === 'blob') {
      let fileName = '导出文件.xlsx'; // 默认文件名，容错处理
      // 解析文件名，做双层容错：先判断header是否存在，再try-catch防止解析失败
      if (response.headers['content-disposition'] || response.headers['Content-Disposition']) {
        try {
          const disposition = response.headers['content-disposition'] || response.headers['Content-Disposition'];
          fileName = decodeURI(disposition.split('filename=')[1]);
          // 兼容部分后端返回的文件名带引号的情况
          fileName = fileName.replace(/"/g, '');
        } catch (err) {
          fileName = '导出文件.xlsx';
        }
      }
      // ✅ 关键：把文件流和文件名挂载到response扩展属性上，不修改返回值类型
      response.fileBlobData = response.data as Blob;
      response.fileDownloadName = fileName;
      // 直接返回原response，完美符合Axios的类型约束，无任何报错！
      return response;
    }

    // ========== ✅ 2. 你原有的所有业务逻辑 - 一行未改，完整保留 ==========
    console.log(response);
    const { data } = response;
    console.log(data);

    // 未登录处理
    if (data.code === 40100) {
      if (
        !response.request.responseURL.includes("user/current") &&
        !window.location.pathname.includes("/user/login")
      ) {
        message.warning('登录状态已过期，请重新登录');
        window.location.href = `/user/login?redirect=${encodeURIComponent(window.location.href)}`;
      }
    }

    // 业务错误处理
    if (data.code && data.code !== 0 && data.code !== 40100) {
      if (data.message) {
        message.error(data.message);
      } else {
        message.error(`请求失败（错误码：${data.code}）`);
      }
    }
    
    // 返回原response，符合类型约束
    return response;
  },
  function (error) {
    // ========== ✅ 3. blob文件下载的异常兼容处理 ==========
    if (error.config && error.config.responseType === 'blob') {
      message.error('文件导出失败，请稍后重试');
      return Promise.reject(error);
    }

    // ========== ✅ 4. 你原有的所有错误处理逻辑 - 一行未改，完整保留 ==========
    let errorMsg = '请求失败，请稍后重试';
    
    if (error.response) {
      const { status, data } = error.response;
      console.error('服务器响应错误：', error.response);
      
      switch (status) {
        case 400:
          errorMsg = data.message || '请求参数错误，请检查输入内容';
          break;
        case 401:
          errorMsg = '登录状态已失效，请重新登录';
          localStorage.removeItem('loginUser');
          localStorage.removeItem('token');
          if (!window.location.pathname.includes("/user/login")) {
            window.location.href = `/user/login?redirect=${encodeURIComponent(window.location.href)}`;
          }
          break;
        case 403:
          errorMsg = '暂无权限访问该资源，请联系管理员';
          break;
        case 404:
          errorMsg = '请求的接口不存在（404）';
          break;
        case 500:
          errorMsg = '服务器内部错误，请稍后重试';
          break;
        case 502:
          errorMsg = '网关错误，服务器暂时不可用';
          break;
        case 503:
          errorMsg = '服务器维护中，请稍后访问';
          break;
        default:
          errorMsg = data.message || `请求失败（状态码：${status}）`;
      }
    } else if (error.request) {
      console.error('网络请求错误：', error.request);
      if (error.code === 'ECONNABORTED') {
        errorMsg = '请求超时，请检查网络或重试';
      } else {
        errorMsg = '网络异常，请检查网络连接';
      }
    } else {
      console.error('请求配置错误：', error.message);
      errorMsg = `请求配置异常：${error.message}`;
    }

    if (errorMsg) {
      message.error(errorMsg);
    }
    return Promise.reject(error);
  }
);

export default myAxios;