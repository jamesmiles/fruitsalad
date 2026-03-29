-- @test classic-004: Towers of Hanoi
-- @expect Move disk 1 from A to C
-- @expect Move disk 2 from A to B
-- @expect Move disk 1 from C to B
-- @expect Move disk 3 from A to C
-- @expect Move disk 1 from B to A
-- @expect Move disk 2 from B to C
-- @expect Move disk 1 from A to C

blend hanoi(n, from, to, aux) {
    if n == 1 {
        display("Move disk {n} from {from} to {to}")
        yield 0
    }
    hanoi(n - 1, from, aux, to)
    display("Move disk {n} from {from} to {to}")
    hanoi(n - 1, aux, to, from)
}

blend main() {
    hanoi(3, "A", "C", "B")
}
