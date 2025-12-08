# 磁力枪Debug检查清单

## 步骤1：检查磁力枪是否正确装备

### 运行游戏后检查：
1. **捡起磁力枪物品**：按F键捡起磁力枪
2. **装备磁力枪**：按R键切换到磁力枪
3. **查看Console**：应该看到 "Magnetic gun equipped - functionality enabled"

### 如果没看到装备信息：
- 检查磁力枪物品是否有`Pickable`组件
- 检查`equippedPrefab`是否正确设置
- 检查磁力枪预制体是否有`MagneticGun`组件

## 步骤2：检查射线检测

### 装备磁力枪后：
1. **瞄准磁力目标**：将摄像机对准磁力目标
2. **点击鼠标左键**：按住鼠标左键
3. **查看Console**：应该看到射线检测信息

### 可能的问题：
- **"No hit detected"**：射线没有击中任何物体
  - 检查磁力目标是否在射线路径上
  - 检查`maxDistance`是否足够
  - 检查`magneticLayers`设置

- **"Hit [物体名] but no MagneticTarget"**：击中了物体但没有磁力组件
  - 检查磁力目标是否有`MagneticTarget`组件
  - 检查`MagneticTarget`的`isMagnetic`是否勾选

## 步骤3：检查磁力目标设置

### 磁力目标必须有的组件：
1. **Rigidbody**：必须有Rigidbody组件
2. **Collider**：必须有Collider组件（不能是Trigger）
3. **MagneticTarget**：必须有MagneticTarget组件
4. **isMagnetic**：MagneticTarget的isMagnetic必须勾选

### 检查方法：
- 选中磁力目标物体
- 查看Inspector中的组件
- 确保所有必需组件都存在且正确设置

## 步骤4：检查磁力求解器

### 场景中必须有：
1. **MagneticSolver**：场景中必须有MagneticSolver组件
2. **MagneticSolver对象**：可以是任意GameObject，但必须有MagneticSolver组件

### 检查方法：
- 在Hierarchy中搜索"MagneticSolver"
- 确保存在且MagneticSolver组件已添加

## 步骤5：常见问题解决

### 问题1：磁力枪没有装备
**解决方案**：
- 确保磁力枪物品有`Pickable`组件
- 确保`equippedPrefab`指向正确的磁力枪预制体
- 确保磁力枪预制体有`MagneticGun`组件

### 问题2：射线检测失败
**解决方案**：
- 检查`magneticLayers`设置（可以设置为"Everything"）
- 检查`maxDistance`是否足够大
- 确保磁力目标有Collider且不是Trigger

### 问题3：磁力目标没有反应
**解决方案**：
- 确保磁力目标有Rigidbody组件
- 确保MagneticTarget的`isMagnetic`勾选
- 确保场景中有MagneticSolver

### 问题4：力太小
**解决方案**：
- 增加`basePullForce`值
- 检查磁力目标的`strength`值
- 确保磁力目标不是Kinematic

## 调试技巧

1. **开启Debug信息**：在MagneticGun组件中勾选`showDebugInfo`
2. **查看Console**：所有调试信息都会显示在Console中
3. **逐步测试**：按照步骤1-4逐步检查每个环节
4. **简化测试**：先用简单的Cube作为磁力目标测试

## 快速测试设置

如果还是有问题，可以尝试这个快速设置：

1. **创建简单的磁力目标**：
   - 创建Cube
   - 添加Rigidbody
   - 添加MagneticTarget组件
   - 勾选isMagnetic

2. **设置磁力枪**：
   - magneticLayers设置为"Everything"
   - maxDistance设置为50
   - basePullForce设置为100

3. **确保有MagneticSolver**：
   - 创建空对象
   - 添加MagneticSolver组件
