-- @test string-004: Anagram Check
-- @expect true
-- @expect false

blend char_code(c) {
    -- Map lowercase letters to 0-25
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
    -1
}

blend is_anagram(s1, s2) {
    if s1.len() != s2.len() {
        yield false
    }

    -- Count character frequencies using a basket of 26 zeros
    fresh counts = []
    each i in 0..26 {
        counts.push(0)
    }

    each i in 0..s1.len() {
        preserve c1 = char_code(s1[i])
        preserve c2 = char_code(s2[i])
        counts[c1] = counts[c1] + 1
        counts[c2] = counts[c2] - 1
    }

    fresh result = true
    each i in 0..26 {
        if counts[i] != 0 {
            result = false
            snap
        }
    }
    result
}

blend main() {
    display(is_anagram("listen", "silent"))
    display(is_anagram("hello", "world"))
}
