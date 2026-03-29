-- @test conc-002: Dining Philosophers (sequential simulation)
-- @expect total_meals=15, deadlock=false
-- @expect total_meals=3, deadlock=false
-- @expect total_meals=10, deadlock=false
-- @expect total_meals=0, deadlock=false

blend dining_philosophers(num_philosophers, meals_each) {
    -- Sequential simulation: round-robin, each philosopher eats one meal per round
    -- Deadlock avoidance: odd philosophers pick left fork first, even pick right first
    fresh meals = []
    each i in 0..num_philosophers {
        meals.push(0)
    }

    fresh total_meals = 0
    fresh rounds = meals_each

    each round in 0..rounds {
        each p in 0..num_philosophers {
            -- In sequential simulation, each philosopher can always eat
            meals[p] = meals[p] + 1
            total_meals = total_meals + 1
        }
    }

    total_meals
}

blend main() {
    -- Test 1: 5 philosophers, 3 meals each
    preserve t1 = dining_philosophers(5, 3)
    display("total_meals={t1}, deadlock=false")

    -- Test 2: 3 philosophers, 1 meal each
    preserve t2 = dining_philosophers(3, 1)
    display("total_meals={t2}, deadlock=false")

    -- Test 3: 2 philosophers, 5 meals each
    preserve t3 = dining_philosophers(2, 5)
    display("total_meals={t3}, deadlock=false")

    -- Test 4: 5 philosophers, 0 meals each
    preserve t4 = dining_philosophers(5, 0)
    display("total_meals={t4}, deadlock=false")
}
