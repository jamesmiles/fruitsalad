-- @test math-006: Sieve of Eratosthenes
-- @expect [2, 3, 5, 7, 11, 13, 17, 19, 23, 29]

blend main() {
    preserve limit = 30
    fresh is_prime = []
    each i in 0..=limit {
        is_prime.push(true)
    }
    is_prime[0] = false
    is_prime[1] = false

    each i in 2..=limit {
        if is_prime[i] {
            fresh j = i * i
            while j <= limit {
                is_prime[j] = false
                j = j + i
            }
        }
    }

    fresh primes = []
    each i in 2..=limit {
        if is_prime[i] {
            primes.push(i)
        }
    }

    display(primes)
}
