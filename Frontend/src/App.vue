<template>
  <div id="app">
    <!-- 直接渲染路由视图，不默认加载布局 -->
    <router-view />
  </div>
</template>

<script setup lang="ts">
import { useLoginUserStore } from "@/store/useLoginUserStore";

// 初始化用户登录状态
const loginUserStore = useLoginUserStore();
loginUserStore.fetchLoginUser();

</script>

<style>
/* 全局样式重置 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html, body, #app {
  height: 100%;
}
</style>