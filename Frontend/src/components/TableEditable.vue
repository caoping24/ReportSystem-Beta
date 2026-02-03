<template>
  <a-tabs type="card" style="max-width: 1800px; margin: 20px auto;">
    <a-tab-pane key="data-edit" tab="小时数据编辑">
      <div class="table-container">
        <div class="date-selector">
          <el-date-picker
            v-model="selectedDate"
            type="date"
            placeholder="选择查询日期"
            @change="fetchTableData"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            :disabled-date="disabledFutureDate"
            :picker-options="{ shortcuts: [{ text: '今天', onClick: () => { selectedDate.value = new Date().toISOString().split('T')[0]; fetchTableData(); } }] }"
          />
          <el-button type="primary" @click="fetchTableData">查询</el-button>
          <!-- 新增重载按钮：样式与查询一致 -->
          <el-button type="primary" @click="reloadTableData">重载</el-button>
        </div>
        <div class="table-scroll-wrapper">
          <el-table
            :data="tableData"
            border
            style="width: 100%; table-layout: fixed;"
            :cell-class-name="cellClassName"
            size="small"
            empty-text="当前日期暂无小时数据" 
          >
            <el-table-column
              v-for="(header, index) in tableHeaders"
              :key="index"
              :prop="header.prop"
              :label="header.label"
              :width="header.prop === 'hour' ? 60 : 90"
              align="center"
            >
              <template #default="scope">
                <template v-if="header.prop === 'hour'">
                  {{ scope.row[header.prop] }}
                </template>
                <template v-else>
                  <template v-if="isCellDisabled(scope.row)">
                    {{ scope.row[header.prop] || '-' }}
                  </template>
                  <template v-else>
                    <el-input
                      v-model="scope.row[header.prop]"
                      size="mini"
                      @blur="handleCellEdit(scope.row, header.prop)"
                      :disabled="isCellDisabled(scope.row)"
                      maxlength="6"
                      style="width: 80px;" 
                    />
                  </template>
                </template>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </a-tab-pane>
    <a-tab-pane key="data-view" tab="数据预览">
      <div style="padding: 20px; text-align: center;">
        数据预览模块（可自定义内容）
      </div>
    </a-tab-pane>
  </a-tabs>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
// 导入核心接口，【ReloadData为重载接口】后续对接后端后删除注释即可
import { Headers, HourData, SaveCell,ReloadData } from "@/api/TableEdit";

// 表格表头类型：prop匹配后端cells的key（cell29/cell30等）
interface TableHeader {
  prop: string;
  label: string;
}

// 后端返回的小时数据项结构（精准适配，全小写）
interface HourDataItem {
  hour: number;
  date: string;
  isNextDay: boolean;
  cells?: Record<string, string>;
}

// 前端表格行类型：继承后端结构+动态字段兼容
interface TableRow extends HourDataItem {
  [key: string]: any;
}

// 【重载接口参数类型】严格匹配后端要求：type固定1，time为日期字符串
interface ReloadDataParams {
  type: number;
  time: string;
}

// 响应式数据
const selectedDate = ref<string>(''); // 选中日期
const tableHeaders = ref<TableHeader[]>([]); // 表格表头
const tableData = ref<TableRow[]>([]); // 表格数据

// 禁用未来日期选择
const disabledFutureDate = (date: Date): boolean => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const selectDate = new Date(date);
  selectDate.setHours(0, 0, 0, 0);
  return selectDate.getTime() > today.getTime();
};

// 判断单元格是否禁用（未来时间禁用，适配次日时间isNextDay）
const isCellDisabled = (row: TableRow): boolean => {
  if (!row.date || row.hour === undefined || row.hour === null) return true;

  const currentTime = new Date().getTime();
  const rowDate = new Date(row.date);
  const targetDate = new Date(rowDate);

  if (row.isNextDay) {
    targetDate.setDate(targetDate.getDate() + 1);
  }
  targetDate.setHours(row.hour, 0, 0, 0);
  const rowRealTime = targetDate.getTime();

  return rowRealTime > currentTime;
};

// 单元格样式：禁用/小时列添加灰色背景
const cellClassName = ({ row, column }: { row: TableRow; column: any }): string => {
  if (column.prop === 'hour') return 'disabled-cell';
  return isCellDisabled(row) ? 'disabled-cell' : '';
};

// 获取表格表头（从后端接口拉取，强制hour列排第一）
const fetchTableHeaders = async (): Promise<void> => {
  try {
    const res = await Headers();
    if (res?.data) {
      tableHeaders.value = res.data;
      const hourHeader = tableHeaders.value.find(item => item.prop === 'hour');
      if (hourHeader) {
        tableHeaders.value = [hourHeader, ...tableHeaders.value.filter(item => item.prop !== 'hour')];
      }
    }
  } catch (error) {
    ElMessage.error('获取表格表头失败，请刷新页面');
    console.error('fetchTableHeaders error:', error);
  }
};

// 获取指定日期的小时数据，解析后端cells字段为表格可渲染的动态字段
const fetchTableData = async (): Promise<void> => {
  if (!selectedDate.value) {
    ElMessage.warning('请先选择查询日期');
    return;
  }

  try {
    const res = await HourData({ date: selectedDate.value });
    const originData = res?.data || [];

    if (originData.length === 0) {
      tableData.value = [];
      ElMessage.info(`【${selectedDate.value}】暂无小时数据`);
      return;
    }

    const formatTableData = originData.map((item: HourDataItem) => {
      if (!item) return { hour: 0, date: selectedDate.value, isNextDay: false, cells: {} } as TableRow;
      const cellData = item.cells || {};
      return { ...item, ...cellData } as TableRow;
    });

    tableData.value = formatTableData;
    ElMessage.success(`【${selectedDate.value}】小时数据加载成功`);
  } catch (error) {
    ElMessage.error('小时数据加载失败，请重试');
    console.error('fetchTableData error:', error);
  }
};

// 单元格编辑失焦保存，仅保存非禁用、非小时列的修改
const handleCellEdit = async (row: TableRow, prop: string): Promise<void> => {
  if (prop === 'hour' || isCellDisabled(row)) return;

  const saveParams = {
    date: row.date,
    hour: row.hour,
    prop: prop,
    value: row[prop] || ''
  };

  try {
    await SaveCell(saveParams);
    ElMessage.success(`已保存：${row.date} ${row.hour}点 - ${prop} 字段`);
  } catch (error) {
    ElMessage.error('单元格数据保存失败，请重试');
    console.error('handleCellEdit error:', error);
  }
};

// 重载表格数据：调用后端重载接口（参数type=1，time=选中日期），重载后刷新表格
const reloadTableData = async (): Promise<void> => {
  if (!selectedDate.value) {
    ElMessage.warning('请先选择查询日期');
    return;
  }

  try {
    ElMessage.info(`正在重载【${selectedDate.value}】数据，请稍候...`);
    // 构造重载参数：严格匹配后端要求，type固定传1
    const reloadParams: ReloadDataParams = {
      type: 1,
      time: selectedDate.value
    };
    // 【对接后端后删除注释】调用重载接口
     await ReloadData(reloadParams);
    // 重载成功后重新拉取数据，保证表格数据最新
    await fetchTableData();
    ElMessage.success(`【${selectedDate.value}】数据重载完成`);
  } catch (error) {
    ElMessage.error('数据重载失败，请重试');
    console.error('reloadTableData error:', error);
  }
};

// 页面挂载初始化：先加载表头，再加载今日数据
onMounted(async () => {
  await fetchTableHeaders();
  selectedDate.value = new Date().toISOString().split('T')[0];
  await fetchTableData();
});
</script>

<style scoped>
:deep(.ant-tabs-card) {
  --ant-tabs-card-head-background: #f8f9fa;
  border-radius: 4px;
}
.table-container { padding: 15px; }
/* 日期选择器+按钮布局，新增重载按钮后间距保持一致 */
.date-selector { margin-bottom: 10px; display: flex; gap: 10px; align-items: center; }
/* 表格横向滚动容器，适配多列 */
.table-scroll-wrapper {
  width: 100%;
  overflow-x: auto;
  scrollbar-width: thin;
  scrollbar-color: #ccc #f5f5f5;
}
.table-scroll-wrapper::-webkit-scrollbar { height: 6px; }
.table-scroll-wrapper::-webkit-scrollbar-thumb { background-color: #ccc; border-radius: 3px; }
/* 表格单元格紧凑样式 */
:deep(.el-table td), :deep(.el-table th) {
  padding: 4px 0 !important;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
:deep(.el-table th .cell) { font-size: 14px; font-weight: 500; }
:deep(.el-table td .cell) { font-size: 13px; }
/* 禁用单元格样式 */
.disabled-cell { background-color: #f5f5f5; color: #999; cursor: not-allowed; }
/* 迷你输入框样式适配 */
:deep(.el-input--mini) { width: 80px !important; }
:deep(.el-input__wrapper) { padding: 0 5px !important; font-size: 13px; }
/* 日期选择器禁用样式优化 */
:deep(.el-picker-panel__content .el-date-table td.disabled) { color: #ccc !important; cursor: not-allowed !important; }
</style>