# The One Billion Row Challenge

At the beginning of January 2024 Gunnar Morling launched The One Billion Row Challenge (1BRC) to the Java Community [on his blog]. The challenge aimed to find Java code that processed one billion rows from a text file and aggregate the values in the fastest time possible. A lot of developers submitted their solutions until Morling closed the challenge at the end of the month and published the [final leaderboards](https://www.morling.dev/blog/1brc-results-are-in/).
After the news of this challenge spread many people built their own solutions using other [languages, databases, and tools](https://github.com/gunnarmorling/1brc/discussions/categories/show-and-tell).

## Basics of running the Challenge

1. Generate the measurements file with 1B rows (just once).
	- **Attention:** This will take a few minutes and the generated file has a size of approx. **12 GB**, so make sure to have enough diskspace.
2. Calculate the average measurement values from the measurements file.
	- Measure the time needed for reading the file and calculating the average. Output of the result values is not part of the challenge. 
3. Optimize the heck out of it to speed up your code!

## Rules and limits

- No external library dependencies may be used.
- Implementations must be provided as a single source file.
- The computation must happen at application _runtime_, i.e. you cannot process the measurements file at _build time_ and just bake the result into the binary.
- Input value ranges are as follows:
    - Station name: non null UTF-8 string of min length 1 character and max length 100 bytes, containing neither `;` nor `\n` characters. (i.e. this could be 100 one-byte characters, or 50 two-byte characters, etc.).
    - Temperature value: non null double between -99.9 (inclusive) and 99.9 (inclusive), always with one fractional digit.
- There is a maximum of 10,000 unique station names.
- Line endings in the file are `\n` characters on all platforms.
- Implementations must not rely on specifics of a given data set, e.g. any valid station name as per the constraints above and any data distribution (number of measurements per station) must be supported.
- The rounding of output values must be done using the semantics of IEEE 754 rounding-direction "roundTowardPositive".