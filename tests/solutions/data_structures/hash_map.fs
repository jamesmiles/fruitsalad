-- @test ds-004: Hash Map with Separate Chaining
-- @expect 1
-- @expect 2

blend char_code(c) {
    if c == "a" { yield 0 }
    if c == "b" { yield 1 }
    if c == "c" { yield 2 }
    if c == "d" { yield 3 }
    if c == "e" { yield 4 }
    if c == "f" { yield 5 }
    if c == "g" { yield 6 }
    if c == "h" { yield 7 }
    if c == "i" { yield 8 }
    if c == "j" { yield 9 }
    if c == "k" { yield 10 }
    if c == "l" { yield 11 }
    if c == "m" { yield 12 }
    if c == "n" { yield 13 }
    if c == "o" { yield 14 }
    if c == "p" { yield 15 }
    if c == "q" { yield 16 }
    if c == "r" { yield 17 }
    if c == "s" { yield 18 }
    if c == "t" { yield 19 }
    if c == "u" { yield 20 }
    if c == "v" { yield 21 }
    if c == "w" { yield 22 }
    if c == "x" { yield 23 }
    if c == "y" { yield 24 }
    if c == "z" { yield 25 }
    0
}

blend hash_key(key, bucket_count) {
    fresh sum = 0
    each i in 0..key.len() {
        sum = sum + char_code(key[i])
    }
    sum % bucket_count
}

blend create_map(bucket_count) {
    fresh buckets = []
    each i in 0..bucket_count {
        buckets.push([])
    }
    buckets
}

blend map_put(buckets, key, value, bucket_count) {
    preserve idx = hash_key(key, bucket_count)
    preserve chain = buckets[idx]

    -- Check if key already exists and update
    fresh found = false
    each i in 0..chain.len() {
        if chain[i][0] == key {
            chain[i][1] = value
            found = true
            snap
        }
    }

    -- If not found, add new entry [key, value]
    if found == false {
        chain.push([key, value])
    }
}

blend map_get(buckets, key, bucket_count) {
    preserve idx = hash_key(key, bucket_count)
    preserve chain = buckets[idx]

    each i in 0..chain.len() {
        if chain[i][0] == key {
            yield chain[i][1]
        }
    }

    yield -1
}

blend main() {
    preserve bucket_count = 8
    fresh buckets = create_map(bucket_count)

    map_put(buckets, "hello", 1, bucket_count)
    map_put(buckets, "world", 2, bucket_count)

    display(map_get(buckets, "hello", bucket_count))
    display(map_get(buckets, "world", bucket_count))
}
