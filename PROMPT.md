We’re going to create a programming language.

It will be an innovative ‘Turing Complete’ 3GL called ‘Fruit Salad’.

It should have a fruit salad theme for declaring types, assigning fields (e.g. types could be called ‘Bowls’?). Exceptions/Errors? Think of something clever. Juice/Juicing, Peel/Peeling should be important. You should attempt to come up with new/innovative language features. The language should be easy to use and understand by modern standards (e.g. memory safe), and be efficient at solving problems.

1. Write a plan including
    1. designing an execution harnesses for both ‘freestanding’ and ‘hosted’ execution environments, these should allow you to execute and reliably capture outputs, logs & memory dumps of programs safely (e.g. without crashing the host)
        1. Consider technologies like QEMU (freestanding emulation)
        2. Docker for user mode (hosted) execution environment
    2. Write a language standard with all the innovative features you’d like to be able to efficiently write programs
    3. Creating a test strategy which includes:
        1. Create a large library of well known algorithms and computer problems using the following sources:
            * "The Algorithms" Project (GitHub): Community-driven repositories featuring standard algorithms with verified unit tests in multiple programming languages.
            * Rosetta Code: A chrestomathy website demonstrating how specific algorithms are implemented and output across hundreds of different languages.
            * Competitive Platforms (LeetCode/HackerRank): Problem databases that verify your custom code against massive, rigorous suites of known inputs and expected outputs.
            * Standard Library Test Suites (CPython/libc++): The official open-source language repositories containing enterprise-grade unit tests for built-in algorithms.
            * Stony Brook Algorithm Repository: An academic directory linking to highly optimized, historically battle-tested algorithmic implementations and benchmark data.
        * NOTE: this library is just a source of problems to solve, not the tests themselves
    4. your overall ‘reward signal’ should be calculated as the total number of passing tests / total number of tests
    5. Finally you’ll need to write the compiler or interpreter (‘fs’), design a process that provides rapid feedback/reward signal, e.g.
        1. Pick a problem from the library, assess whether the Fruit Salad language has the constructs required to write a solution to the selected problem
            1. If it doesn’t enhance the Fruit Salad language spec
        2. Write a solution to the selected problem in fruit salad, adding it to the test suite
            1. document the expected behaviour/output of each program inline
            2. create ‘negative’ test cases, programs that shouldn’t compile or run, and ensuring our compiler or interpreter produces correct & meaningful errors
        3. Execute test programs
        4. Execute test programs using test harnesses
        5. Review output/logs/memory dumps of failing tests
            1. note: memory dumps and logs are for diagnostic purposes, not a reward signal
        6. If all tests pass 
            1. Check CI from previous commits/ and address any issues
            2. Push to main
            3. Goto 1
        7. Else (tests fail)
            1. Write/modify/improve the FS compiler/interpreter or;
            2. Revise the language spec & compiler/interpreter
            3. Goto 3.
    6. Tests should run on GitHub Actions (CI) & locally
    7. Don’t stop until the problems in the library have been implemented and pass in FruitSalad
    8. Once the compiler is complete, write a FruitSalad compiler (‘ffs’) in FruitSalad
    9. Don’t cheat!
2. Push the plan to main
3. Start executing the plan and don’t break my computer :)