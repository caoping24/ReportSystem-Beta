<template>
  <div id="userManagePage">
    <!-- 新增 Tabs 组件实现页面切换 -->
    <a-tabs default-active-key="1" type="card">
      <!-- 第一个标签页 -->
      <a-tab-pane tab="日报表" key="1">
        <a-table :columns="columns" :data-source="data1" bordered>
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'createTime'">
              {{ dayjs(record.createTime).format("YYYY-MM-DD HH:mm:ss") }}
            </template>
            <template v-else-if="column.key === 'action'">
              <a-button @click="downloadExcel(1, record.id)">下载</a-button>
            </template>
          </template>
        </a-table>
      </a-tab-pane>

      <!-- 第二个标签页 -->
      <a-tab-pane tab="周报表" key="2">
        <a-table :columns="columns" :data-source="data2" bordered>
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'createTime'">
              {{ dayjs(record.createTime).format("YYYY-MM-DD HH:mm:ss") }}
            </template>
            <template v-else-if="column.key === 'action'">
              <a-button @click="downloadExcel(2, record.id)">下载</a-button>
            </template>
          </template>
        </a-table>
      </a-tab-pane>

      <!-- 第三个标签页 -->
      <a-tab-pane tab="月报表" key="3">
        <a-table :columns="columns" :data-source="data3" bordered>
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'createTime'">
              {{ dayjs(record.createTime).format("YYYY-MM-DD HH:mm:ss") }}
            </template>
            <template v-else-if="column.key === 'action'">
              <a-button @click="downloadExcel(3, record.id)">下载</a-button>
            </template>
          </template>
        </a-table>
      </a-tab-pane>

      <!-- 第四个标签页 -->
      <a-tab-pane tab="年报表" key="4">
        <a-table :columns="columns" :data-source="data4" bordered>
          <template #bodyCell="{ column, record }">
            <template v-if="column.dataIndex === 'createTime'">
              {{ dayjs(record.createTime).format("YYYY-MM-DD HH:mm:ss") }}
            </template>
            <template v-else-if="column.key === 'action'">
              <a-button @click="downloadExcel(4, record.id)">下载</a-button>
            </template>
          </template>
        </a-table>
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script lang="ts" setup>
import { searchReports, downloadReport } from "@/api/user"; // 导入下载接口
import { message } from "ant-design-vue";
import { ref } from "vue";
import dayjs from "dayjs";

// 为四个表格分别定义数据源
const data1 = ref([]);
const data2 = ref([]);
const data3 = ref([]);
const data4 = ref([]);

// 下载Excel方法
const downloadExcel = async (tabKey: number, id: string) => {
  if (!id) {
    message.warning("ID 不能为空");
    return;
  }
  
  try {
    // 调用下载接口
    const res = await downloadReport(Number(id), tabKey);
    
    // 处理Blob响应，创建下载链接
    const blob = new Blob([res.data], { 
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
    });
    
    // 获取文件名（从响应头提取或自定义）
    const contentDisposition = res.headers['content-disposition'];
    let fileName = `报表.xlsx`;
    if (contentDisposition) {
      const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match && utf8Match[1]) {
      // 解码UTF-8编码的文件名（兼容中文）
      fileName = decodeURIComponent(utf8Match[1]);
    } else {
      // 兼容非标准格式：filename="中文名称.xlsx" 或 filename=中文名称.xlsx
      const normalMatch = contentDisposition.match(/filename=([^;]+)/i);
      if (normalMatch && normalMatch[1]) {
        // 移除引号 + 解码（处理中文编码）
        fileName = decodeURIComponent(normalMatch[1].replace(/['"]/g, ''));
      }
      }
    }
    
    // 创建临时链接并触发下载
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    
    // 清理资源
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
    
    message.success("下载成功");
  } catch (error) {
    console.error("下载接口调用失败：", error);
    message.error("下载失败：网络异常或接口错误");
  }
};

// 表格列配置（复用）
const columns = [
  {
    title: "ID",
    dataIndex: "id",
    key: "id",
  },
  {
    title: "创建时间",
    dataIndex: "createTime",
    key: "createTime",
  },
  {
    title: "操作",
    key: "action",
    width: 120,
  },
];

// 通用数据获取方法，tabKey 区分不同表格
const fetchData = async (tabKey: number) => {
  try {
    // 实际项目中可根据 tabKey 传递不同参数给接口
    const res = await searchReports(tabKey);
    
    if (res.data && res.data.data) {
      // 根据 tabKey 赋值给对应数据源
      switch (tabKey) {
        case 1:
          data1.value = res.data.data;
          break;
        case 2:
          data2.value = res.data.data;
          break;
        case 3:
          data3.value = res.data.data;
          break;
        case 4:
          data4.value = res.data.data;
          break;
      }
    } else {
      message.error(`获取${tabKey}号表格数据失败`);
    }
  } catch (error) {
    console.error(`获取${tabKey}号表格数据异常：`, error);
    message.error(`获取${tabKey}号表格数据失败：网络异常`);
  }
};

// 页面初始化时加载四个表格数据
fetchData(1);
fetchData(2);
fetchData(3);
fetchData(4);
</script>

<style scoped>
#userManagePage {
  padding: 20px;
}
.ant-tabs-card > .ant-tabs-nav .ant-tabs-tab {
  padding: 12px 24px;
}
.ant-table {
  margin-top: 16px;
}
</style>