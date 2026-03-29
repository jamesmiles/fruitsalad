-- @test type-001: Bowl with Computed Fields
-- @expect 15
-- @expect 16

bowl Rectangle {
    width: Date,
    height: Date,
}

blend area(r) {
    r.width * r.height
}

blend perimeter(r) {
    2.0 * (r.width + r.height)
}

blend main() {
    preserve r = Rectangle { width: 5.0, height: 3.0 }
    display(area(r))
    display(perimeter(r))
}
