import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'
import Antd from 'ant-design-vue';
import 'ant-design-vue/dist/reset.css';

// 导入Store并恢复状态
import { useLoginUserStore } from '@/store/useLoginUserStore'

const app = createApp(App)

// 安装插件
app.use(createPinia())
app.use(router)
app.use(Antd)

// 恢复登录状态（双重保障）
const loginUserStore = useLoginUserStore()
loginUserStore.restoreLoginUser()

app.mount('#app')