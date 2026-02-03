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
// 导入TableEdit.ts中对接后端的三个核心接口
import { Headers, HourData, SaveCell } from "@/api/TableEdit";

// 前端表格表头类型（匹配后端TableHeaderDto）
interface TableHeader {
  prop: string;
  label: string;
}

// 后端HourDataDto对应的前端类型（首字母大写匹配后端返回，Cells改为可选）
interface HourDataItem {
  Hour: number;
  Date: string;
  IsNextDay: boolean;
  Cells?: Record<string, string>; // 关键修改：Cells设为可选属性，解决TS类型缺失错误
}

// 前端表格行最终类型：转换后端字段为小写，继承可选的Cells
interface TableRow extends Omit<HourDataItem, 'Hour' | 'Date' | 'IsNextDay'> {
  hour: number;
  date: string;
  isNextDay: boolean;
  [key: string]: any; // 兼容动态的cell1/cell2等字段
}

// 响应式数据定义
const selectedDate = ref<string>(''); // 选中的查询日期
const tableHeaders = ref<TableHeader[]>([]); // 表格表头数据
const tableData = ref<TableRow[]>([]); // 表格核心数据

// 禁用未来日期选择（原逻辑不变）
const disabledFutureDate = (date: Date): boolean => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const selectDate = new Date(date);
  selectDate.setHours(0, 0, 0, 0);
  return selectDate.getTime() > today.getTime();
};

// 判断单元格是否禁用（未来时间禁用，原逻辑不变）
const isCellDisabled = (row: TableRow): boolean => {
  // 基础容错：关键数据缺失直接禁用
  if (!row.date || row.hour === undefined || row.hour === null) return true;

  const currentTime = new Date().getTime(); // 当前时间戳
  const rowDate = new Date(row.date);       // 选中的日期对象
  const targetDate = new Date(rowDate);     // 待计算的真实时间对象

  // 若标记为次日时间，日期+1天（解决8点时间歧义）
  if (row.isNextDay) {
    targetDate.setDate(targetDate.getDate() + 1);
  }

  // 构建真实时间（时分秒毫秒置零，仅比较日期+小时）
  targetDate.setHours(row.hour, 0, 0, 0);
  const rowRealTime = targetDate.getTime();

  // 未来时间返回true（禁用），过去/当前时间返回false（可编辑）
  return rowRealTime > currentTime;
};

// 单元格样式类名（原逻辑不变）
const cellClassName = ({ row, column }: { row: TableRow; column: any }): string => {
  // 小时列和禁用单元格添加灰色背景样式
  if (column.prop === 'hour') return 'disabled-cell';
  return isCellDisabled(row) ? 'disabled-cell' : '';
};

// 从后端接口获取表格表头（替换原本地mock）
const fetchTableHeaders = async (): Promise<void> => {
  try {
    const res = await Headers();
    // 接口返回有效数据则赋值，否则为空数组
    if (res?.data) {
      tableHeaders.value = res.data;
    }
  } catch (error) {
    ElMessage.error('获取表格表头失败，请刷新页面');
    console.error('fetchTableHeaders error:', error);
  }
};

// 从后端接口获取指定日期的小时数据（替换原本地mock，增加多层容错）
const fetchTableData = async (): Promise<void> => {
  // 未选择日期则提示
  if (!selectedDate.value) {
    ElMessage.warning('请先选择查询日期');
    return;
  }

  try {
    // 调用后端HourData接口，传递日期参数
    const res = await HourData({ date: selectedDate.value });
    // 容错1：后端返回data为空/undefined时，设为空数组
    const originData = res?.data || [];

    // 无数据时清空表格并提示
    if (originData.length === 0) {
      tableData.value = [];
      ElMessage.info(`【${selectedDate.value}】暂无小时数据`);
      return;
    }

    // 转换后端数据结构为前端表格可识别的格式
    const formatTableData = originData.map((item: HourDataItem) => {
      // 容错2：单个数据项为空时，返回默认空行
      if (!item) {
        return {
          hour: 0,
          date: selectedDate.value,
          isNextDay: false,
          Cells: {}
        };
      }
      // 容错3：Cells为undefined/null时，设为空对象（核心解决TS类型错误）
      const cellData = item.Cells || {};

      // 后端字段转前端小写，展开单元格数据
      return {
        hour: item.Hour,
        date: item.Date,
        isNextDay: item.IsNextDay,
        Cells: cellData, // 显式赋值，符合类型定义
        ...cellData      // 展开Cells中的cell1/cell2等字段，适配表格渲染
      } as TableRow;
    });

    // 赋值给表格响应式数据
    tableData.value = formatTableData;
    ElMessage.success(`【${selectedDate.value}】小时数据加载成功`);
  } catch (error) {
    ElMessage.error('小时数据加载失败，请重试');
    console.error('fetchTableData error:', error);
  }
};

// 单元格编辑失焦后，调用后端接口保存修改
const handleCellEdit = async (row: TableRow, prop: string): Promise<void> => {
  // 小时列/禁用单元格不执行保存
  if (prop === 'hour' || isCellDisabled(row)) return;

  // 构造后端SaveCell接口所需的参数（首字母大写匹配后端RequestDto）
  const saveParams = {
    Date: row.date,
    Hour: row.hour,
    Prop: prop,
    Value: row[prop] || '' // 空值兜底，避免传递undefined
  };

  try {
    // 调用后端保存接口
    await SaveCell(saveParams);
    ElMessage.success(`已保存：${row.date} ${row.hour}点 - ${prop} 字段`);
  } catch (error) {
    ElMessage.error('单元格数据保存失败，请重试');
    console.error('handleCellEdit error:', error);
  }
};

// 页面挂载时初始化：先加载表头，再默认选中今日并加载数据
onMounted(async () => {
  await fetchTableHeaders(); // 优先加载表头，避免表格无列
  // 默认选中当前日期（格式：YYYY-MM-DD）
  selectedDate.value = new Date().toISOString().split('T')[0];
  await fetchTableData(); // 加载今日的小时数据
});
</script>

<style scoped>
/* 原样式完全保留，无修改 */
:deep(.ant-tabs-card) {
  --ant-tabs-card-head-background: #f8f9fa;
  border-radius: 4px;
}
.table-container { padding: 15px; }
.date-selector { margin-bottom: 10px; display: flex; gap: 10px; align-items: center; }
.table-scroll-wrapper {
  width: 100%;
  overflow-x: auto;
  scrollbar-width: thin;
  scrollbar-color: #ccc #f5f5f5;
}
.table-scroll-wrapper::-webkit-scrollbar { height: 6px; }
.table-scroll-wrapper::-webkit-scrollbar-thumb { background-color: #ccc; border-radius: 3px; }
:deep(.el-table td), :deep(.el-table th) {
  padding: 4px 0 !important;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
:deep(.el-table th .cell) { font-size: 14px; font-weight: 500; }
:deep(.el-table td .cell) { font-size: 13px; }
.disabled-cell { background-color: #f5f5f5; color: #999; cursor: not-allowed; }
:deep(.el-input--mini) { width: 80px !important; }
:deep(.el-input__wrapper) { padding: 0 5px !important; font-size: 13px; }
:deep(.el-picker-panel__content .el-date-table td.disabled) { color: #ccc !important; cursor: not-allowed !important; }
</style>