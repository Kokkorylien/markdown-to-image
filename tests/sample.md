# 把一条回复，完整留成图片

复制 Markdown，按 **Ctrl+Alt+M**，表格、代码和公式就会排版成一张 PNG。

> 所有转换都在本机完成。浅色和深色主题可以在托盘设置里切换。

## 表格也能一起保存

| 内容 | 支持方式 | 说明 |
| :--- | :---: | :--- |
| 中文与列表 | ✓ | 保留标题层次、粗体、引用和链接 |
| 代码块 | ✓ | 常见语言自动高亮，长行换行 |
| 长回复 | ✓ | 图片高度随内容增长 |
| 数学公式 | ✓ | 使用本地 KaTeX 排版 |

## 代码保持可读

```javascript
async function saveMarkdown(markdown) {
  const result = await renderLocally(markdown);
  console.log('图片已保存', result.path);
  return result;
}
```

```text
这是一行很长的文本，用来检查导出时是否会在图片边缘被裁切：https://example.com/a-very-long-path/abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789
```

## 数学公式

行内公式 $E=mc^2$，以及另一种写法 \(a^2+b^2=c^2\)。

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$

\[
\int_0^1 x^2\,dx = \frac{1}{3}
\]

## 宽表格不会从右侧截断

| 项目 | 第一季度 | 第二季度 | 第三季度 | 第四季度 | 全年备注 | 状态 |
| --- | --- | --- | --- | --- | --- | --- |
| 本地转换 | 中文说明 | 中文说明 | 中文说明 | 中文说明 | 一次复制，一次快捷键，整个回复保存 | 已完成 |

1. 点击回复的“复制”。
2. 按快捷键，等待保存通知。
3. 点击通知查看图片，或在托盘打开保存目录。

---

**结束标记：图片应当完整包含这一行。**
