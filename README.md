# 画线救狗头

一款 2D 画线保护类小游戏。玩家通过手指/鼠标绘制线条，阻挡蜜蜂攻击狗头。蜜蜂使用 A* 寻路算法自动追踪狗头，玩家需要在关键通道处画线拦截。坚持 10 秒即通关。

## 游戏玩法

- **按住屏幕/鼠标**画线，松手后线条受重力下落
- 线条会阻挡蜜蜂路径，蜜蜂碰到线条会被推开
- 蜜蜂碰到狗头 → 游戏失败
- 坚持 10 秒 → 通关

## 项目结构

```
Assets/Scripts/
├── Core/           # 核心系统（GameManager, LevelManager, PhysicsInit 等）
├── Bee/            # 蜜蜂 AI（A* 寻路, 行为控制, 生成器）
├── Draw/           # 画线系统（LineDrawer, RDP 简化）
└── UI/             # 界面管理（IMGUI 倒计时 + 弹窗）
```

## 技术栈

- **引擎**：Tuanjie 1.9.1（Unity 2022.3）
- **物理**：Unity 2D Physics
- **寻路**：自实现 A* 算法（0.4 精度网格 + 8 方向 + 路径平滑）
- **画线**：LineRenderer + 复合碰撞体（Rigidbody2D + 子 BoxCollider2D）

## 运行方式

1. 用 Tuanjie/Unity 打开项目
2. 打开 `Assets/Scenes/MainScene.unity`
3. 点击 Play 运行
