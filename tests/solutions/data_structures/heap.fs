-- @test ds-005: Min-Heap with Insert and Extract-Min
-- @expect 1
-- @expect 2
-- @expect 3

blend swap(arr, i, j) {
    arr.swap(i, j)
}

blend sift_up(heap, idx) {
    fresh i = idx
    while i > 0 {
        preserve parent = (i - 1) / 2
        if heap[i] < heap[parent] {
            swap(heap, i, parent)
            i = parent
        } else {
            snap
        }
    }
}

blend sift_down(heap, idx, size) {
    fresh i = idx
    while true {
        preserve left = 2 * i + 1
        preserve right = 2 * i + 2
        fresh smallest = i

        if left < size && heap[left] < heap[smallest] {
            smallest = left
        }
        if right < size && heap[right] < heap[smallest] {
            smallest = right
        }

        if smallest != i {
            swap(heap, i, smallest)
            i = smallest
        } else {
            snap
        }
    }
}

blend heap_insert(heap, val) {
    heap.push(val)
    sift_up(heap, heap.len() - 1)
}

blend heap_extract_min(heap) {
    preserve min_val = heap[0]
    preserve last = heap.pop()
    if heap.len() > 0 {
        heap[0] = last
        sift_down(heap, 0, heap.len())
    }
    min_val
}

blend main() {
    fresh heap = []
    preserve values = [4, 2, 7, 1, 5, 3]

    each i in 0..values.len() {
        heap_insert(heap, values[i])
    }

    display(heap_extract_min(heap))
    display(heap_extract_min(heap))
    display(heap_extract_min(heap))
}
