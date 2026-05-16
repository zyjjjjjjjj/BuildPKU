# JSON 配置编写原则

本文档说明当前建筑、卡牌和 buff 系统的配置方式。建筑数据写在 `json_data/Buildings.json`，卡牌数据写在 `json_data/Cards.json`，buff 数据写在 `json_data/Buffs.json`。建筑配置负责“有哪些建筑、初始能不能建造、建筑数值是什么”，卡牌负责“什么时候出现、显示什么、绑定哪些效果”，buff 负责“真正执行什么效果”。

## 整体流程

1. 游戏启动时，`BuildingMgr` 从 `Buildings.json` 中读取 `InitiallyAvailable` 为 `true` 的建筑，放入开局可建造列表。
2. 每隔若干年，`CardMgr` 从 `Cards.json` 中筛选可用卡牌。
3. 玩家选择卡牌后，如果卡牌的 `CardType` 是 `Buff`，系统会读取它的 `BuffIds`。
4. 每个 `BuffId` 会在 `Buffs.json` 中找到对应的 buff 配置。
5. `BuffMgr.AddBuff` 根据 `BuffType` 执行具体效果。
6. 如果 buff 是 `UnlockBuilding`，会把指定建筑加入可建造列表，右侧建筑栏会同步刷新。
7. 被选择过的卡牌会显示在右侧卡牌栏，卡牌栏 tooltip 会显示这张卡绑定的 buff 描述和持续时间。

## Building 编写原则

建筑配置示例：

```json
{
  "Id": "canteen",
  "InitiallyAvailable": false,
  "Name": "食堂",
  "Research": 0,
  "Education": 0,
  "Environment": 25,
  "Population": 0,
  "Cost": 3000,
  "Benefit": 500,
  "Destroyable": true,
  "Limit": -1,
  "UpgradeTo": "",
  "UpgradeCost": 0
}
```

字段说明：

| 字段 | 类型 | 作用 |
| --- | --- | --- |
| `Id` | string | 建筑唯一 id，需要和 `buildings/{Id}.tscn` 文件名对应。 |
| `InitiallyAvailable` | bool | 是否开局显示在右侧建筑栏。`true` 表示开局可建造，`false` 表示需要通过卡牌或其他逻辑解锁。 |
| `Name` | string | 建筑显示名称。 |
| `Research` | float | 建筑提供的科研产出。 |
| `Education` | float | 建筑提供的教学产出。 |
| `Environment` | float | 建筑提供的环境产出，可以为负数。 |
| `Population` | float | 建筑提供的人口。 |
| `Cost` | float | 建造花费。 |
| `Benefit` | float | 建筑每年提供的收益。 |
| `Destroyable` | bool | 是否允许拆除。未填写时默认为 `true`。 |
| `Limit` | int | 最大建造数量。`-1` 表示不限制，正数表示最多建造几个。 |
| `UpgradeTo` | string | 升级目标建筑 id。没有升级目标时写空字符串或不写。 |
| `UpgradeCost` | float | 升级花费。未填写时默认为 `0`。 |

建议：

- 需要玩家通过卡片解锁的建筑，把 `InitiallyAvailable` 写成 `false`。
- 初始地图上已经摆好的建筑，不一定要 `InitiallyAvailable: true`；这个字段只控制右侧建筑栏是否开局可建造。
- `Id` 修改后，需要同步检查 `buildings/{Id}.tscn`、解锁 buff 的 `Targets`、相邻加成中的 `DestBuildingId` / `SrcBuildingId`。
- 如果建筑有限制数量，达到 `Limit` 后会从右侧建筑栏移除。

## Card 编写原则

卡牌配置示例：

```json
{
  "Id": "startup_grant",
  "Name": "启动资金",
  "Description": "立刻获得一笔建设资金",
  "CardType": "Buff",
  "Rarity": "Common",
  "MinYear": 1905,
  "Repeatable": true,
  "BuffIds": ["startup_grant_buff"]
}
```

字段说明：

| 字段 | 类型 | 作用 |
| --- | --- | --- |
| `Id` | string | 卡牌唯一 id，不要重复。 |
| `Name` | string | 卡牌显示名称。 |
| `Description` | string | 卡牌显示描述。 |
| `CardType` | string | 当前生效的是 `Buff`。只有 `Buff` 类型会读取 `BuffIds` 并执行 buff。 |
| `Rarity` | string | 稀有度字段，目前主要用于配置表达，可写 `Common`、`Epic` 等。 |
| `MinYear` | int | 最早出现年份。当前年份大于等于它时才可能被抽到。 |
| `Repeatable` | bool | `false` 表示被选择后不再进入候选池；`true` 表示可以重复抽到。 |
| `BuffIds` | string array | 这张卡会触发的 buff id 列表。可以绑定一个或多个 buff。 |

建议：

- `Id` 使用小写英文和下划线，例如 `startup_grant`。
- 一张卡可以绑定多个 buff，适合组合效果；如果只是简单效果，优先一张卡绑定一个 buff。
- 临时或一次性效果也放在 `BuffIds` 里，保持卡牌入口统一。

## Buff 通用字段

buff 配置示例：

```json
{
  "Id": "startup_grant_buff",
  "Description": "立刻获得 5000 资金",
  "DurationYears": 0,
  "BuffType": "InstantMoney",
  "AddValue": 5000
}
```

字段说明：

| 字段 | 类型 | 作用 |
| --- | --- | --- |
| `Id` | string | buff 唯一 id，需要被卡牌的 `BuffIds` 引用。 |
| `Description` | string | buff 描述，会在卡牌栏 tooltip 中显示。 |
| `DurationYears` | int | 持续时间。`-1` 表示永久，`0` 表示立即生效，正数表示持续若干年。 |
| `BuffType` | string | buff 类型，决定由哪段逻辑执行。 |
| `Targets` | string array | 目标列表，部分 buff 类型需要。`ScoreWeightMultiplier` / `AdjBonus` 中表示属性列表，`UnlockBuilding` 中表示建筑 id 列表。 |
| `Multiplier` | float | 乘法倍率，部分 buff 类型需要。 |
| `AddValue` | float | 加法数值，当前 `InstantMoney` 使用。 |
| `DestBuildingId` | string | 目标建筑 id，当前 `AdjBonus` 使用。 |
| `SrcBuildingId` | string | 相邻来源建筑 id，当前 `AdjBonus` 使用。 |

`DurationYears` 规则：

- `-1`：永久效果。
- `0`：立即生效效果，不进入持续 buff 衰减逻辑。
- `> 0`：持续指定年数，到期后由 `BuffMgr` 移除。

## 当前支持的 BuffType

### ScoreWeightMultiplier

用于改变科研、教学、环境评分权重。

```json
{
  "Id": "research_priority_buff",
  "Description": "科研权重提高 20%",
  "DurationYears": -1,
  "BuffType": "ScoreWeightMultiplier",
  "Targets": ["Research"],
  "Multiplier": 1.2
}
```

规则：

- 会进入 `BuffMgr` 的持续权重 buff 列表。
- `ScoreMgr` 重算分数时会读取这些 buff。
- `Targets` 当前支持：
  - `Research`
  - `Education`
  - `Environment`
- `Multiplier` 是倍率，不是百分比。提高 20% 写 `1.2`，提高 50% 写 `1.5`。

### AdjBonus

用于建筑相邻加成。意思是：当某类目标建筑旁边有某类来源建筑时，目标建筑的指定产出获得倍率加成。

```json
{
  "Id": "biology_century_buff",
  "Description": "实验室与绿地相邻时，科研产出提高 20%",
  "DurationYears": -1,
  "BuffType": "AdjBonus",
  "Targets": ["Research"],
  "Multiplier": 1.2,
  "DestBuildingId": "lab",
  "SrcBuildingId": "green"
}
```

规则：

- 会进入 `BuffMgr` 的相邻加成 buff 字典。
- 添加后会立刻尝试作用于已经存在的目标建筑。
- 持续时间到期后，会让受影响的建筑重新计算相邻加成。
- `DestBuildingId` 是被加成的建筑。
- `SrcBuildingId` 是需要相邻的建筑。
- `Targets` 由建筑加成逻辑读取，常见值包括 `Research`、`Education`、`Environment`、`Population`、`Benefit`。

### InstantMoney

用于立刻增加金钱。

```json
{
  "Id": "startup_grant_buff",
  "Description": "立刻获得 5000 资金",
  "DurationYears": 0,
  "BuffType": "InstantMoney",
  "AddValue": 5000
}
```

规则：

- 被卡牌触发时，`BuffMgr` 会立刻调用 `MoneyMgr.AddMoney(AddValue)`。
- 不会进入持续 buff 列表。
- 不会随年份衰减。
- 仍然会跟随卡牌显示在右侧卡牌栏。
- 推荐 `DurationYears` 写 `0`，这样 UI 会显示“立即生效”。
- `AddValue` 可以是负数，但如果要做扣钱效果，建议另起更明确的 `BuffType`，避免配置语义混乱。

### UnlockBuilding

用于通过卡牌解锁新的可建造建筑。

```json
{
  "Id": "unlock_canteen_buff",
  "Description": "解锁食堂建造",
  "DurationYears": 0,
  "BuffType": "UnlockBuilding",
  "Targets": ["canteen"]
}
```

规则：

- 被卡牌触发时，`BuffMgr` 会把 `Targets` 中的建筑 id 加入 `BuildingMgr.AvailableBuildingIds`。
- 右侧建筑栏会收到可建造建筑列表变化信号，并立刻显示新建筑。
- 如果目标建筑已经在可建造列表中，不会重复添加。
- 推荐 `DurationYears` 写 `0`，因为解锁是一次性立即效果，不需要随年份衰减。
- `Targets` 可以写多个建筑 id，例如 `["canteen", "dpt_office"]`，用于一张卡同时解锁多个建筑。

配套卡牌示例：

```json
{
  "Id": "unlock_canteen",
  "Name": "后勤扩建",
  "Description": "解锁食堂建造",
  "CardType": "Buff",
  "Rarity": "Common",
  "MinYear": 1905,
  "Repeatable": false,
  "BuffIds": ["unlock_canteen_buff"]
}
```

## 新增卡牌效果的推荐步骤

1. 判断新效果是否已有 `BuffType` 能表达。
2. 如果能表达，只需要在 `Buffs.json` 新增 buff，并在 `Cards.json` 新增或修改卡牌引用它。
3. 如果不能表达，需要在 `BuffData` 中确认是否已有合适参数字段。
4. 在 `BuffMgr.AddBuff` 中新增 `BuffType` 分支，执行对应效果。
5. 如果是持续效果，需要把它保存到某个 active buff 容器，并在年份推进时处理过期。
6. 如果 UI 需要特殊显示，在 `CardChoicePanel` 或 `BuffSideBar` 中补充格式化逻辑。

## 配置检查清单

- `Buildings.json`、`Cards.json` 和 `Buffs.json` 都是合法 JSON。
- 建筑 `Id` 不重复，并且能找到对应的 `buildings/{Id}.tscn`。
- 需要开局建造的建筑写 `InitiallyAvailable: true`；需要卡牌解锁的建筑写 `InitiallyAvailable: false`。
- 卡牌 `Id` 不重复。
- buff `Id` 不重复。
- 每个 `BuffIds` 都能在 `Buffs.json` 找到。
- `UnlockBuilding` 的每个 `Targets` 都能在 `Buildings.json` 找到。
- `CardType` 写成 `Buff`，否则不会执行 `BuffIds`。
- `Repeatable` 按设计填写，强力永久卡通常建议 `false`。
- `DurationYears` 和 `BuffType` 语义一致：立即效果写 `0`，永久效果写 `-1`，临时效果写正数。
- 倍率类字段使用倍率值，例如 `1.2`，不要写 `20`。
