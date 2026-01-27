<template>
  <a-table 
    :columns="columns" 
    :data-source="dataSource" 
    bordered
    :pagination="paginationConfig"
    :row-key="(record) => record.id"
  >
    <template #bodyCell="{ column, record, index }">
      <template v-if="column.key === 'index'">
        {{ (paginationParams.pageIndex - 1) * paginationParams.pageSize + index + 1 }}
      </template>
      <template v-else-if="column.dataIndex === 'createTime'">
        {{ dayjs(record.createdtime).format("YYYY-MM-DD HH:mm:ss") }}
      </template>
      <template v-else-if="column.key === 'action'">
        <a-button @click="handleDownload(record.createdtime)">下载</a-button>
      </template>
    </template>
  </a-table>
</template>

<script lang="ts" setup>
import dayjs from 'dayjs';
import { defineProps, defineEmits } from 'vue';
import type { TableProps } from 'ant-design-vue/es/table';
import type { PaginationProps } from 'ant-design-vue/es/pagination';

interface ReportTableProps {
  tabKey: string;
  columns: TableProps['columns'];
  dataSource: any[];
  paginationConfig: PaginationProps;
  paginationParams: {
    pageIndex: number;
    pageSize: number;
  };
}

const emit = defineEmits<{
  (e: 'download', tabKey: string, createTime: string): void;
}>();

const props = defineProps<ReportTableProps>();

const handleDownload = (createTime: string | Date) => {
  // 格式化时间为 YYYY-MM-DD HH:mm:ss 格式
  const formattedTime = dayjs(createTime).format("YYYY-MM-DD HH:mm:ss");
  emit('download', props.tabKey, formattedTime);
};
</script>

<script lang="ts">
import { defineComponent } from 'vue';

export default defineComponent({
  name: 'ReportTable'
});
</script>

<style scoped>
/* 表格配色调整 - 表头背景改为纯白色 */
::v-deep .ant-table {
  --ant-table-header-text-color: #003399; /* 表头文字色（保留深蓝） */
  --ant-table-border-color: #e8f4fc; /* 表格边框色（浅蓝系） */
  --ant-table-row-hover-bg: #f0f8ff; /* 行hover背景（保留浅蓝） */
}

/* 表头样式 - 核心修改：背景改为纯白色 */
::v-deep .ant-table-thead > tr > th {
  background: #ffffff !important; /* 表头背景纯白 */
  color: #003399; /* 表头文字深蓝 */
  border-bottom: 2px solid #00AEEF; /* 表头下边框（浅蓝飘带色） */
}

/* 下载按钮配色 */
::v-deep .ant-table-cell .ant-btn {
  color: #003399;
  border-color: #003399;
  background: #fff;
}
::v-deep .ant-table-cell .ant-btn:hover {
  color: #fff;
  background: #00AEEF;
  border-color: #00AEEF;
}

/* 表格行边框 */
::v-deep .ant-table-tbody > tr > td {
  border-bottom: 1px solid #e8f4fc;
}
</style>