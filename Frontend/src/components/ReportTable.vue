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
        <a-button @click="handleDownload(record.id)">下载</a-button>
      </template>
    </template>
  </a-table>
</template>

<!-- 保留原有 setup 脚本 -->
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
  (e: 'download', tabKey: string, id: string): void;
}>();

const props = defineProps<ReportTableProps>();

const handleDownload = (id: string) => {
  emit('download', props.tabKey, id);
};
</script>

<!-- 新增这部分：显式声明默认导出，解决 TS 识别问题 -->
<script lang="ts">
import { defineComponent } from 'vue';

// 显式导出组件，让 TS 识别默认导出
export default defineComponent({
  name: 'ReportTable' // 可选：给组件命名，方便调试
});
</script>