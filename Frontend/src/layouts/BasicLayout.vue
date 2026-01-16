<template>
  <div class="layout-container">
    <!-- 左侧菜单栏 -->
    <aside class="sidebar">
      <!-- 侧边栏头部 -->
      <!-- <div class="sidebar-header">
        <img class="logo" src="../assets/logo.png" alt="logo" />
        <div class="title">用户中心</div>
      </div> -->
      
      <!-- 侧边栏菜单 -->
      <a-menu
        v-model:selectedKeys="current"
        mode="vertical"
        :items="items"
        @click="doMenuClick"
        class="sidebar-menu"
      />

      <!-- 退出登录按钮 -->
      <div class="logout-btn">
        <a-button type="text" @click="handleLogout">退出登录</a-button>
      </div>
    </aside>

    <!-- 右侧主内容区 -->
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script lang="ts" setup>
import { ref } from "vue";
import { MenuProps } from "ant-design-vue";
import { useRouter } from "vue-router";
import { useLoginUserStore } from "@/store/useLoginUserStore";
import { message } from "ant-design-vue";
// 导入退出登录接口
import { userLogout } from "@/api/user";

const router = useRouter();
const loginUserStore = useLoginUserStore();

// 菜单配置（key对应路由路径）
const items = ref<MenuProps["items"]>([
  { key: "/app/home", label: "主页", title: "主页" },
   { key: "/app/admin/userManage", label: "用户管理", title: "用户管理" },
]);

// 菜单点击跳转
const doMenuClick = ({ key }: { key: string }) => {
  router.push(key);
};

// 同步路由选中状态
const current = ref<string[]>(["/app/home"]);
router.afterEach((to) => {
  current.value = [to.path];
});

// 退出登录（完整逻辑，修复 TS 类型错误）
const handleLogout = async () => {
  try {
    // 1. 调用后端退出登录接口
    await userLogout({});
    
    // 2. 清空Store中的用户信息（修复 void 类型判断问题）
    if (typeof loginUserStore.clearLoginUser === 'function') {
      loginUserStore.clearLoginUser();
    } else {
      loginUserStore.loginUser = { userName: "未登录" };
    }
    
    // 3. 清空本地存储
    localStorage.removeItem("loginUser");
    localStorage.removeItem("token");
    
    // 4. 清空Cookie
    clearAllCookies();
    
    // 5. 提示并跳转
    message.success("退出成功");
    router.replace({ path: "/user/login" });
  } catch (error) {
    // 接口失败也清空状态
    console.error("退出登录接口调用失败：", error);
    if (typeof loginUserStore.clearLoginUser === 'function') {
      loginUserStore.clearLoginUser();
    } else {
      loginUserStore.loginUser = { userName: "未登录" };
    }
    localStorage.removeItem("loginUser");
    localStorage.removeItem("token");
    clearAllCookies();
    message.success("已退出登录");
    router.replace({ path: "/user/login" });
  }
};

// 辅助函数：清空所有Cookie
const clearAllCookies = () => {
  const cookies = document.cookie.split(";");
  for (let i = 0; i < cookies.length; i++) {
    const cookie = cookies[i];
    const eqPos = cookie.indexOf("=");
    const name = eqPos > -1 ? cookie.substr(0, eqPos).trim() : cookie.trim();
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; domain=${window.location.hostname}`;
  }
};
</script>

<style scoped>
/* 样式部分不变，省略 */
.layout-container {
  display: flex;
  height: 100vh;
  overflow: hidden;
}
.sidebar {
  width: 180px;
  height: 100%;
  background-color: #0f172a;
  color: #e2e8f0;
  box-shadow: 2px 0 12px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
}
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 12px;
  height: 60px;
  border-bottom: 1px solid #1e293b;
}
.logo {
  height: 30px;
  margin-right: 8px;
}
.title {
  color: #f8fafc;
  font-size: 16px;
  font-weight: 600;
}
.sidebar-menu {
  border-right: none !important;
  padding: 12px 0;
  flex: 1;
}
.logout-btn {
  padding: 12px;
  border-top: 1px solid #1e293b;
}
.main-content {
  flex: 1;
  padding: 24px;
  background-color: #f8fafc;
  overflow-y: auto;
}
:deep(.ant-menu-vertical .ant-menu-item) {
  height: 44px;
  line-height: 44px;
  margin: 0 8px !important;
  border-radius: 6px;
  padding-left: 20px !important;
  color: #e2e8f0 !important;
}
:deep(.ant-menu-item-selected) {
  background-color: #3b82f6 !important;
  color: #ffffff !important;
}
:deep(.ant-menu-item:hover) {
  background-color: #1e293b !important;
  color: #ffffff !important;
}
:deep(.ant-menu-title-content) {
  font-size: 14px;
  font-weight: 500;
}
:deep(.ant-btn-text) {
  color: #e2e8f0 !important;
  display: block;
  width: 100%;
  text-align: center;
}
:deep(.ant-btn-text:hover) {
  background-color: #1e293b !important;
  color: #fff !important;
}
</style>