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
  "UpgradeCost": {
	"canteen 1": 1000,
	"canteen 2": 1000,
  }
}
```

字段说明：

| 字段 | 类型 | 作用 |
| --- | --- | --- |
| `Id` | string | 建筑唯一 id，需要和 `buildings/{Id}.tscn` 文件名对应。 |
| `Name` | string | 建筑显示名称。 |
| `Research` | float | 建筑提供的科研产出。 |
| `Education` | float | 建筑提供的教学产出。 |
| `Environment` | float | 建筑提供的环境产出，可以为负数。 |
| `Population` | float | 建筑提供的人口。 |
| `Cost` | float | 建造花费。 |
| `Benefit` | float | 建筑每年提供的收益。 |
| `Destroyable` | bool | 是否允许拆除。未填写时默认为 `true`。 |
| `Limit` | int | 最大建造数量。`-1` 表示不限制，正数表示最多建造几个。 |
| `UpgradeCost` | float | 升级目标建筑id及花费。可以不填。 |

建议：
- `Id` 修改后，需要同步检查 `buildings/{Id}.tscn`、Initial_Buildings.json, 解锁建筑buff和相邻加成buff中的 `DestBuildingId` / `SrcBuildingId`。

## Region 编写原则

id必须以region_开头

## Card 编写原则

卡牌配置示例：

```json
{
  "Id": "academic_reputation",
  "Name": "学术声望",
  "Description": "科研与教学评分权重提高 20%。",
  "CardType": "Buff",
  "Rarity": "Epic",
  "MinYear": 1905,
  "Repeatable": false,
  "Buffs": {
	"weight_multiplier_buff_1":{
	  "Target": "Education",
	  "Value": 20
	},
	"weight_multiplier_buff_2":{
	  "Target": "Research",
	  "Value": 20
	},
	"unlock_building":{
	  "DestBuildingIdOrTag":"canteen"
	},
	"adj_bonus_buff":{
	  "Target":"Research",
	  "Value": 20,
	  "DestBuildingIdOrTag": "lab",
	  "SrcBuildingIdOrTag": "green"
	}
  }
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
| `Buffs` | string array | 这张卡会触发的 buff 列表。可以绑定一个或多个buff, 如需绑定多个同id buff, buff名称后面需加下划线和数字后缀, 如上面示例中的weight_multiplier_buff_1  每个buff需要填写相关参数, 目前包括StartYear, DurationYears, Value, Target, SrcBuildingIdOrTag, DestBuildingIdOrTag。可以不填使用默认值, 具体说明如下.|
| `StartYear` | int | 获得卡片后多少年buff开始生效, 默认为0即立即开始, 不可为负数 |
| `DurationYears` | int | 生效时长, 默认为-1即永久 |
| `Target` | string | 影响的属性, 只能为Research, Education, Environment, Population, Benefit中的一个, 注意拼写和首字母大写 |
| `Value` | int | 数值类参数, 必须为整数默认为0, buff的description字段{Value}的实际值, 例如权重提升buff中"Description": "{Target}评分权重提高{Value}%。", 则若想权重提高20%, Value需设置为20而非1.2!!! |
| `SrcBuildingIdOrTag` | string | 建筑id类参数, 默认为空, 目前仅相邻加成类buff需要填写 |
| `DestBuildingIdOrTag` | string | 建筑id类参数, 默认为空, 目前相邻加成类buff和解锁建筑类需要填写 |

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
  "BuffType": "InstantMoney",
}
```

字段说明：

| 字段 | 类型 | 作用 |
| --- | --- | --- |
| `Id` | string | buff 唯一 id，需要被卡牌的 `BuffIds` 引用。 |
| `Description` | string | buff 描述，会在卡牌栏 tooltip 中显示。 |

## 新增卡牌效果的推荐步骤

1. 判断新效果是否已有buff能表达。
2. 如果能表达，只需要在修改Cards.json即可, 如不能表达, 需联系程序新增buff类型
