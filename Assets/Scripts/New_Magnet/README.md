# 磁力枪系统使用说明

## 方案2：磁力功能在磁力枪上

### 文件结构
- `MagneticGun.cs` - 磁力枪组件（附加在磁力枪预制体上）
- `MagneticTarget.cs` - 磁力目标组件（附加在被吸引的物体上）
- `MagneticSolver.cs` - 磁力求解器（处理磁力目标之间的相互作用）

### 使用方法

#### 1. 设置磁力枪物品
1. 创建一个磁力枪的3D模型
2. 在磁力枪预制体上添加 `MagneticGun` 组件
3. 配置磁力枪参数：
   - `cam`: 摄像机引用
   - `fireOrigin`: 发射点Transform
   - `maxDistance`: 最大作用距离
   - `basePullForce`: 基础吸力
   - `magneticLayers`: 磁力层遮罩

#### 2. 设置可拾取物品
1. 在磁力枪物品上添加 `Pickable` 组件
2. 设置 `itemName`（如："Magnetic Gun"）
3. 设置 `equippedPrefab` 为包含 `MagneticGun` 组件的预制体

#### 3. 设置磁力目标
1. 在需要被吸引的物体上添加 `MagneticTarget` 组件
2. 配置参数：
   - `isMagnetic`: 是否具有磁性
   - `typeId`: 类型ID（相同类型相斥，不同类型相吸）
   - `strength`: 磁力强度
   - `range`: 作用范围

#### 4. 设置磁力求解器
1. 在场景中创建一个空物体
2. 添加 `MagneticSolver` 组件
3. 配置全局参数

### 工作原理

1. **捡起磁力枪**：玩家捡起磁力枪物品
2. **装备磁力枪**：切换到Pick模式，磁力枪预制体被实例化
3. **激活功能**：`PlayerController` 自动调用 `MagneticGun.OnEquipped()`
4. **使用磁力**：按住鼠标左键瞄准磁力目标，松开停止
5. **卸下磁力枪**：切换到其他模式或空手，`PlayerController` 自动调用 `MagneticGun.OnUnequipped()`

### 优势
- ✅ 磁力枪自带磁力功能，符合逻辑
- ✅ 每个磁力枪可以有不同的参数
- ✅ 装备时自动激活，卸下时自动停用
- ✅ 不需要在PlayerController上添加额外组件

### 注意事项
- 确保磁力枪预制体包含 `MagneticGun` 组件
- 确保磁力目标包含 `MagneticTarget` 组件
- 确保场景中有 `MagneticSolver` 来处理磁力相互作用
