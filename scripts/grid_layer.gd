@tool
extends Node2D

@export var grid_width := 8:
	set(value):
		grid_width = value
		queue_redraw()

@export var grid_height := 6:
	set(value):
		grid_height = value
		queue_redraw()

@export var cell_size := 64:
	set(value):
		cell_size = value
		queue_redraw()

@export var show_grid := true:
	set(value):
		show_grid = value
		queue_redraw()

var selected_building_id: String = ""
var occupied := {}

@onready var building_layer: Node2D = $"../buildingMgr"

func _ready():
	if not Engine.is_editor_hint():
		create_grid()

	queue_redraw()

func create_grid():
	for child in get_children():
		child.queue_free()

	for y in range(grid_height):
		for x in range(grid_width):
			create_cell(Vector2i(x, y))

func create_cell(grid_idx: Vector2i):
	var area := Area2D.new()
	area.name = "Cell_%s_%s" % [grid_idx.x, grid_idx.y]
	area.position = Vector2(grid_idx.x * cell_size, grid_idx.y * cell_size)

	var collision := CollisionShape2D.new()
	var shape := RectangleShape2D.new()
	shape.size = Vector2(cell_size-0.1, cell_size-0.1)
	collision.shape = shape
	area.add_child(collision)

	var highlight := Polygon2D.new()
	highlight.name = "highlight"
	highlight.polygon = PackedVector2Array([
		Vector2(0, 0),
		Vector2(cell_size, 0),
		Vector2(cell_size, cell_size),
		Vector2(0, cell_size)
	])
	highlight.color = Color(0, 1, 0, 0.35)
	highlight.visible = false
	highlight.z_index = 100
	area.add_child(highlight)

#	area.input_event.connect(_on_cell_input_event.bind(grid_idx))
#	area.mouse_entered.connect(_on_cell_mouse_entered.bind(area))
#	area.mouse_exited.connect(_on_cell_mouse_exited.bind(area))
	area.area_entered.connect(_on_cell_mouse_entered.bind(area))
	area.area_exited.connect(_on_cell_mouse_exited.bind(area))

	add_child(area)

func _on_cell_input_event(_viewport, event, _shape_idx, grid_idx: Vector2i):
	if event is InputEventMouseButton:
		if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			on_cell_clicked(grid_idx)
			
func _on_cell_mouse_entered(other_area, area: Area2D):
	print("entered ", area.name)
	if(not other_area.is_in_group("tempBuilding")):
		return
	area.get_node("highlight").visible = true
	return
	
func _on_cell_mouse_exited(other_area, area: Area2D):
	print("exited ", area.name)
	if(not other_area.is_in_group("tempBuilding")):
		return
	area.get_node("highlight").visible = false
	return

func on_cell_clicked(grid_idx: Vector2i):
	if(selected_building_id == ""):
		return
	if occupied.has(grid_idx):
		print("这里已经有建筑")
		return

	occupied[grid_idx] = selected_building_id
	get_node("Cell_%s_%s" % [grid_idx.x, grid_idx.y]).get_node("highlight").color = Color(1, 0, 0, 0.35)
	spawn_building(grid_idx, selected_building_id)
	print("放置建筑：", selected_building_id, " at ", grid_idx)

func spawn_building(grid_idx: Vector2i, building_id: String):
	var building_scene = load("res://scenes/building.tscn")
	var building = building_scene.instantiate()
	building.position = Vector2(
			position.x + grid_idx.x * cell_size,
			position.y + grid_idx.y * cell_size 
		)
	print("spawn ", building_id, " at ", building.position)
	building_layer.add_child(building)

func _draw():
	if not show_grid:
		return

	for x in range(grid_width + 1):
		var from := Vector2(x * cell_size, 0)
		var to := Vector2(x * cell_size, grid_height * cell_size)
		draw_line(from, to, Color.WHITE, 3.0)

	for y in range(grid_height + 1):
		var from := Vector2(0, y * cell_size)
		var to := Vector2(grid_width * cell_size, y * cell_size)
		draw_line(from, to, Color.WHITE, 3.0)
