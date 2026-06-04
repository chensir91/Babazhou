# 改动日志 v1.36

- 2026年06月04日，修复开发者模式选人界面空白：startDevMode()跳过ban阶段导致pool未初始化，补pool=ROSTER.map(r=>({...r}))
- 2026年06月04日，修复规则/角色图鉴弹窗在首页无法显示（弹窗从battle div内移至全局#app下）
