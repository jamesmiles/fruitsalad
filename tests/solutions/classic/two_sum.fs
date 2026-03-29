-- @test classic-003: Two Sum
-- @expect [0, 1]

blend two_sum(nums, target: Apple) {
    each i in 0..nums.len() {
        each j in i + 1..nums.len() {
            if nums[i] + nums[j] == target {
                display([i, j])
            }
        }
    }
}

blend main() {
    preserve nums = [2, 7, 11, 15]
    two_sum(nums, 9)
}
