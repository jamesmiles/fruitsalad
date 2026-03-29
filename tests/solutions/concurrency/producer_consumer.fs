-- @test conc-001: Producer-Consumer (sequential simulation)
-- @expect consumed=[1, 2, 3, 4, 5]
-- @expect consumed=[10, 20, 30]
-- @expect consumed_total=3
-- @expect consumed=[]

blend produce_consume(buffer_size, items) {
    -- Sequential simulation: producer fills buffer, consumer drains
    fresh buffer = []
    fresh consumed = []
    fresh prod_idx = 0
    fresh cons_idx = 0

    while cons_idx < items.len() || prod_idx < items.len() {
        -- Producer: add to buffer if space available and items remain
        while prod_idx < items.len() && buffer.len() < buffer_size {
            buffer.push(items[prod_idx])
            prod_idx = prod_idx + 1
        }
        -- Consumer: drain one item from buffer
        if buffer.len() > 0 {
            preserve item = buffer[0]
            -- shift buffer left
            fresh new_buffer = []
            each i in 1..buffer.len() {
                new_buffer.push(buffer[i])
            }
            buffer = new_buffer
            consumed.push(item)
            cons_idx = cons_idx + 1
        }
    }
    consumed
}

blend main() {
    -- Test 1: buffer_size=3, produce=[1,2,3,4,5], consumers=1
    preserve result1 = produce_consume(3, [1, 2, 3, 4, 5])
    display("consumed={result1}")

    -- Test 2: buffer_size=1, produce=[10,20,30], consumers=1
    preserve result2 = produce_consume(1, [10, 20, 30])
    display("consumed={result2}")

    -- Test 3: buffer_size=5, produce=[1,2,3], consumers=2 (total count)
    preserve result3 = produce_consume(5, [1, 2, 3])
    display("consumed_total={result3.len()}")

    -- Test 4: buffer_size=2, produce=[], consumers=1
    preserve result4 = produce_consume(2, [])
    display("consumed={result4}")
}
