# The One Billion Row Challenge

At the beginning of January 2024 Gunnar Morling launched The One Billion Row Challenge (1BRC) to the Java Community on his [blog](https://www.morling.dev/blog/one-billion-row-challenge/) and [GitHub](https://github.com/gunnarmorling/1brc). The challenge aimed to find Java code that processed one billion rows from a text file and aggregate the values in the fastest time possible. A lot of developers submitted their solutions until Morling closed the challenge at the end of the month and published the [final leaderboards](https://www.morling.dev/blog/1brc-results-are-in/).
After the news of this challenge spread many people built their own solutions using other [languages, databases, and tools](https://github.com/gunnarmorling/1brc/discussions/categories/show-and-tell).

## Basics of running the challenge

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

## Setup
Old notebook - specs later

## File generation
The logic to generate the rows for the measurements isn't too complicated but writing the file might take a lot of time. So before generating the final measurements file with 1B rows (~12GB) it would be best to improve the code first and test it with a smaller amount of rows.

The first version uses a simple StreamWriter which writes one random line at a time.
```csharp
using (var writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
writer.NewLine = "\n";

	for (var i = 0; i < rowCount; i++) {
		var line = GetLine(names);
		writer.WriteLine(line);
	}
}
```
This will be the base line with 10M rows (~550MB) for further improvements.

|                                             | Duration  |(new-old)/old*100% | Commit |
|---------------------------------------------|-----------|-------------------|--------|
| StreamWriter.WriteLine(line as string)      | 00:30:288 |        0,00%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/f20bdce347f4ec549f1cc0eeb20785c9807db7c1/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write(line as byte array)        | 00:23:201 |      -23,40%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/a8993ae4264db8d9e07f05d7dec939078dd51183/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write + 10 lines as byte array   | 00:21:569 |      -28,79%      ||
| FileStream.Write + 100 lines as byte array  | 00:18:716 |      -38,21%      ||
| FileStream.Write + 1k lines as byte array   | 00:16:937 |      -44,08%      | [Link] (https://github.com/thoeltig/CodingChallenge.1BRC/blob/c18ad89905c30196cdde95f5c5f4e0f97c86e32e/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs)|
| FileStream.Write + 2k lines as byte array   | 00:17:426 |      -42,26%      ||
| FileStream.Write + 2.5k lines as byte array | 00:18:244 |      -39,76%      ||

From the results it is clear that writing a byte array of multiple lines to the FileStream has the best performance. 1000 lines with random line length seems to have a slightly faster performance but might only be on my device and the optimal number will vary from one device to the next depending on the IO bottleneck.
After a bit of CPU and IO monitoring I noticed that IO utilization is close to the maximum through out the file generation process. Generating the random names and final lines in parallel didn't really make any difference but maybe it will be more obvious if a greater row size.

