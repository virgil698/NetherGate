# NetherGate 许可证说明

## 📜 开源许可证：LGPL-3.0

NetherGate 使用 **GNU 宽通用公共许可证第 3 版（LGPL-3.0）** 进行授权。

---

## ✅ 你可以做什么

### 1. **自由使用**
- ✅ 在个人或商业环境中使用 NetherGate
- ✅ 修改源代码以满足你的需求
- ✅ 分发修改后的版本

### 2. **插件开发**
- ✅ 开发私有插件（不需要开源）
- ✅ 开发商业插件（不需要开源）
- ✅ 销售你的插件
- ✅ 使用任何许可证发布你的插件

### 3. **集成到项目**
- ✅ 通过 **动态链接**（DLL/NuGet 包）集成到闭源项目
- ✅ 在专有软件中使用 NetherGate.API

---

## ⚠️ 你需要遵守的规则

### 1. **如果你修改了 NetherGate 核心代码**

如果你修改了 `NetherGate.Core`、`NetherGate.API` 或 `NetherGate.Host` 的源代码：

- ⚠️ **必须** 以 LGPL-3.0 或兼容许可证发布修改后的代码
- ⚠️ **必须** 提供源代码或获取源代码的方式
- ⚠️ **必须** 保留原始的版权声明和许可证信息
- ⚠️ **必须** 明确说明你做了哪些修改

**示例：**
```
✅ 正确：Fork NetherGate → 修改代码 → 以 LGPL-3.0 在 GitHub 发布
❌ 错误：Fork NetherGate → 修改代码 → 闭源商业软件中使用
```

### 2. **如果你静态链接 NetherGate**

如果你将 NetherGate 的代码直接编译到你的应用程序中（静态链接）：

- ⚠️ **必须** 提供用于重新链接的对象代码（.obj/.o 文件）或源代码
- ⚠️ **必须** 允许用户替换 NetherGate 库并重新链接你的应用

**推荐做法：** 使用 **动态链接**（DLL/NuGet 包）避免这些要求。

### 3. **如果你分发 NetherGate**

如果你分发 NetherGate（包括二进制或修改版本）：

- ⚠️ **必须** 附带 LICENSE 文件
- ⚠️ **必须** 提供源代码或获取源代码的链接
- ⚠️ **必须** 明确说明你的修改（如果有）

---

## 🔓 插件开发者的自由

### **插件不受 LGPL-3.0 约束**

作为插件开发者，你的插件通过 NetherGate 的插件 API（`IPlugin` 接口）与核心交互，这属于 **动态链接** 和 **独立作品**。

**你可以：**
- ✅ 使用任何许可证发布插件（MIT、Apache、专有、商业等）
- ✅ 不公开插件源代码
- ✅ 销售插件
- ✅ 在插件中使用任何第三方库（遵守该库的许可证）

**原因：**
- 插件是 **独立的作品**，不是 NetherGate 的衍生作品
- 插件通过定义良好的 API 接口与 NetherGate 交互
- LGPL-3.0 明确允许这种使用方式

**类比：**
- Linux（GPL）允许专有软件运行在其上
- Qt（LGPL）允许专有软件动态链接

---

## 📊 使用场景示例

### ✅ 场景 1：开发私有插件

**你的行为：**
- 使用 `dotnet add package NetherGate.API` 引用 API
- 开发私有插件用于自己的服务器
- 不分发插件

**许可证要求：** 无特殊要求

---

### ✅ 场景 2：开发商业插件

**你的行为：**
- 使用 NetherGate.API 开发插件
- 以商业许可证销售插件
- 不公开插件源代码

**许可证要求：** 无特殊要求（动态链接）

**建议：**
- 在插件文档中注明："本插件基于 NetherGate（LGPL-3.0）平台开发"
- 提供 NetherGate 的下载链接：https://github.com/virgil698/NetherGate

---

### ✅ 场景 3：分发修改后的 NetherGate

**你的行为：**
- Fork NetherGate 仓库
- 修改核心代码（如添加新功能）
- 分发修改后的版本

**许可证要求：**
- ✅ 在 GitHub 或其他平台发布修改后的源代码
- ✅ 使用 LGPL-3.0 或兼容许可证
- ✅ 在 README 中说明你的修改
- ✅ 保留原始版权声明

**示例 README：**
```markdown
# NetherGate Fork - Enhanced Edition

本项目是 [NetherGate](https://github.com/virgil698/NetherGate) 的修改版本。

**修改内容：**
- 添加了 XXX 功能
- 优化了 YYY 性能

**许可证：** LGPL-3.0（继承自原项目）
```

---

### ✅ 场景 4：在专有软件中使用

**你的行为：**
- 在闭源商业软件中集成 NetherGate
- 通过 DLL 或 NuGet 包动态链接 NetherGate.API

**许可证要求：**
- ✅ 在软件的"关于"或"第三方许可证"页面中声明使用了 NetherGate
- ✅ 附带 NetherGate 的 LICENSE 文件副本
- ✅ 提供 NetherGate 源代码的链接（https://github.com/virgil698/NetherGate）

**示例第三方声明：**
```
本软件使用以下开源组件：

- NetherGate (LGPL-3.0)
  https://github.com/virgil698/NetherGate
  Copyright (C) 2025 virgil698
```

---

## ❌ 不允许的行为

### ❌ 1. 去除版权声明

```csharp
// ❌ 错误：删除了版权声明
namespace YourApp
{
    // 从 NetherGate 复制的代码，但删除了版权信息
}
```

### ❌ 2. 闭源分发修改版本

```
❌ 错误：修改 NetherGate 核心代码 → 编译成二进制 → 闭源分发
✅ 正确：修改 NetherGate 核心代码 → 在 GitHub 发布源代码 → 分发
```

### ❌ 3. 将 LGPL 代码用于专有软件而不声明

```
❌ 错误：在专有软件中使用 NetherGate，但不提供许可证信息
✅ 正确：在专有软件的许可证页面中声明使用了 NetherGate（LGPL-3.0）
```

---

## 📚 LGPL-3.0 vs GPL-3.0

**为什么选择 LGPL-3.0 而不是 GPL-3.0？**

| 特性 | GPL-3.0 | LGPL-3.0 |
|------|---------|----------|
| 核心代码修改 | 必须开源 | 必须开源 |
| 动态链接 | 可能需要开源 | **不需要开源** |
| 插件开发 | 可能需要开源 | **不需要开源** |
| 商业使用 | 受限 | **自由** |

**LGPL-3.0 的优势：**
- ✅ 保护核心项目的开源性
- ✅ 允许插件开发者自由选择许可证
- ✅ 促进商业和社区生态系统
- ✅ 与企业友好

---

## 🔗 相关资源

### 官方文档
- [LGPL-3.0 完整文本（英文）](https://www.gnu.org/licenses/lgpl-3.0.html)
- [LGPL-3.0 完整文本（中文）](https://www.gnu.org/licenses/lgpl-3.0.zh-cn.html)
- [GNU 许可证常见问题](https://www.gnu.org/licenses/gpl-faq.zh-cn.html)

### NetherGate 许可证
- [LICENSE 文件](../LICENSE)
- [源代码头文件模板](../LICENSE_HEADER)

### 其他参考
- [SPDX 许可证标识符](https://spdx.org/licenses/LGPL-3.0-or-later.html)
- [Choose a License](https://choosealicense.com/licenses/lgpl-3.0/)

---

## 💬 常见问题

### Q1: 我可以在不开源的情况下销售基于 NetherGate 的插件吗？

**A**: ✅ **可以**。插件通过定义的 API 与 NetherGate 交互，属于独立作品，不受 LGPL-3.0 约束。你可以使用任何许可证发布插件，包括专有商业许可证。

---

### Q2: 我修改了 NetherGate 核心代码，必须开源吗？

**A**: ✅ **是的**。如果你分发修改后的版本，必须以 LGPL-3.0 或兼容许可证发布源代码。

**例外：** 如果你只是内部使用（不分发），则不需要公开源代码。

---

### Q3: 我可以将 NetherGate 集成到专有软件中吗？

**A**: ✅ **可以**，但有条件：

- **动态链接（推荐）**：通过 DLL 或 NuGet 包引用，无需开源你的软件，但需声明使用了 NetherGate。
- **静态链接**：需要提供重新链接的方式（对象代码或源代码），或者开源你的软件。

---

### Q4: 我需要在插件中包含 LICENSE 文件吗？

**A**: ⚠️ **不强制，但推荐**。如果你的插件使用了 NetherGate.API，建议在插件文档或 README 中注明：

```
本插件基于 NetherGate（LGPL-3.0）平台开发。
NetherGate: https://github.com/virgil698/NetherGate
```

---

### Q5: LGPL-3.0 与 MIT/Apache 许可证兼容吗？

**A**: 
- **MIT → LGPL**: ✅ 兼容（可以在 LGPL 项目中使用 MIT 代码）
- **Apache 2.0 → LGPL**: ✅ 兼容（需保留 Apache 的 NOTICE）
- **LGPL → MIT**: ❌ 不兼容（不能将 LGPL 代码改为 MIT）

---

### Q6: 如果我只是使用 NetherGate 运行我的服务器，有许可证限制吗?

**A**: ❌ **没有**。仅使用软件（不分发、不修改）没有任何限制。

---

## 📧 联系方式

如果你对许可证有任何疑问，可以：

1. **提交 Issue**：https://github.com/virgil698/NetherGate/issues
2. **参考官方 FAQ**：https://www.gnu.org/licenses/gpl-faq.zh-cn.html
3. **咨询法律顾问**（如果涉及商业用途）

---

## ⚖️ 免责声明

本文档旨在帮助理解 LGPL-3.0 许可证，但 **不构成法律建议**。如有法律问题，请咨询专业律师。

最终以 [LICENSE 文件](../LICENSE) 中的 LGPL-3.0 完整文本为准。

---

**NetherGate - 自由、开放、强大** 🌐

