import { defineStore } from "pinia";
import { ref } from "vue";
import { getCurrentUser } from "@/api/user";

export const useLoginUserStore = defineStore("loginUser", () => {
  // 初始化：优先从本地存储恢复，无则用默认值
  const loginUser = ref<any>(
    localStorage.getItem("loginUser") 
      ? JSON.parse(localStorage.getItem("loginUser")!) 
      : { userName: "未登录" }
  );

  // 从接口获取当前用户信息（并持久化）
  async function fetchLoginUser() {
    try {
      const res = await getCurrentUser();
      if (res.data.code === 0 && res.data.data) {
        console.log("接口获取用户信息：", res.data.data);
        loginUser.value = res.data.data;
        // 同步到本地存储
        localStorage.setItem("loginUser", JSON.stringify(res.data.data));
        // 如果有token，单独存储
        if (res.data.data.token) {
          localStorage.setItem("token", res.data.data.token);
        }
      }
    } catch (error) {
      console.error("获取当前用户信息失败：", error);
    }
  }

  // 设置登录用户（同步到本地存储）
  function setLoginUser(newLoginUser: any) {
    loginUser.value = newLoginUser;
    localStorage.setItem("loginUser", JSON.stringify(newLoginUser));
    // 存储token（如果有）
    if (newLoginUser.token) {
      localStorage.setItem("token", newLoginUser.token);
    }
  }

  // 清除登录状态
  function clearLoginUser() {
    loginUser.value = { userName: "未登录" };
    localStorage.removeItem("loginUser");
    localStorage.removeItem("token");
  }

  // 手动恢复登录状态
  function restoreLoginUser() {
    const savedUser = localStorage.getItem("loginUser");
    if (savedUser) {
      loginUser.value = JSON.parse(savedUser);
    }
  }
 
  return { 
    loginUser, 
    setLoginUser, 
    fetchLoginUser, 
    clearLoginUser,
    restoreLoginUser 
  };
});
