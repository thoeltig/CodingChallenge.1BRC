# 1️⃣🐝🏎️ The One Billion Row Challenge
At the beginning of January 2024 Gunnar Morling launched The One Billion Row Challenge (1BRC) to the Java Community on his [blog](https://www.morling.dev/blog/one-billion-row-challenge/) and [GitHub](https://github.com/gunnarmorling/1brc). The challenge aimed to find Java code that processed one billion rows from a text file and aggregate the values in the fastest time possible. A lot of developers submitted their solutions until Morling closed the challenge at the end of the month and published the [final leaderboards](https://www.morling.dev/blog/1brc-results-are-in/).
After the news of this challenge spread many people built their own solutions using other [languages, databases, and tools](https://github.com/gunnarmorling/1brc/discussions/categories/show-and-tell).

## Basics of running the challenge
1. Generate the measurements file with 1B rows (just once).
2. Calculate the min, max & average values from the measurements file.
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

**Attention:**
- The original generation used a [list of weather station names](https://github.com/gunnarmorling/1brc/blob/main/data/weather_stations.csv) and selected 10k random names from it. The shortest name I found is 3 bytes and the longest is 24 bytes long.
	- This results in a line range of 3-24 bytes name + 1 byte separator + 1-5 bytes for the decimal + 1 byte new line = 6-31 bytes per line. Total file size is 6-31 GB, average 18,5 GB.
	- The original post contained a warning that the generated file will be approximately 12 GB in size which would mean that most names are 5-9 bytes long.
- The extended challenge added the generic weather station names with 1-100 bytes in length.
	- This results in a line range of 1-100 bytes name + 1 byte separator + 1-5 bytes for the decimal + 1 byte new line = 3-106 bytes per line. Total file size is 3-106 GB, average 54,5 GB.

## Setup
Old notebook
- Intel Core i5-4200U - 2.3 GHz
- DDR3 - 8GB RAM - 1600 MHz
- Toshiba MQ01ABF050 - Read 100 MB/s Write 96 MB/s

## Writing & reading a file
There are a couple of classes that could be used to read & write data to & from a file:
- _FileInfo.CreateText_
- _File.Create_
- _File.CreateText_
- _File.ReadAllText_
- _StreamWriter_
- _StreamReader_
- _FileStream_

After an initial test with reduced data the _FileStream_ with default settings was the clear winner. 
Because I wanted to understand why that was the case I took a deep dive in the documentation, code and performance tests for a couple of days.
The collected informations and results can be found [here](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/FileReadAndWritePerformanceTests.md). 

## File generation V1
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
This will be the base line with **10M rows (~550MB)** for further improvements.

|                                             | Duration  |(new-old)/old*100% | Commit |
|---------------------------------------------|-----------|-------------------|--------|
| StreamWriter.WriteLine(line as string)      | 00:30:288 | base line	      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/f20bdce347f4ec549f1cc0eeb20785c9807db7c1/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write(line as byte array)        | 00:23:201 |      -23,40%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/a8993ae4264db8d9e07f05d7dec939078dd51183/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write + 10 lines as byte array   | 00:21:569 |      -28,79%      ||
| FileStream.Write + 100 lines as byte array  | 00:18:716 |      -38,21%      ||
| FileStream.Write + 1k lines as byte array   | 00:16:937 |      -44,08%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/c18ad89905c30196cdde95f5c5f4e0f97c86e32e/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs)|
| FileStream.Write + 2k lines as byte array   | 00:17:426 |      -42,26%      ||
| FileStream.Write + 2.5k lines as byte array | 00:18:244 |      -39,76%      ||

From the results it is clear that writing a byte array of multiple lines to the FileStream has the best performance. 1000 lines with random line length seems to have a slightly faster performance but might only be on my device and the optimal number will vary from one device to the next depending on the IO bottleneck and the used buffer size.
- The buffer size of the FileStream can also be adjusted. The default is 4096 and increasing it might help. 
- FileStream doesn't support parallel file writes but the random names and final lines can be generated in parallel.

After a bit of [refactoring](https://github.com/thoeltig/CodingChallenge.1BRC/blob/2ba8b8ee1a633fadb83977bb10ce99e2bd691250/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) I was ready to run a test with a couple of combinations:

| Buffer size + line count					  | Duration  |(new-old)/old*100% |
|---------------------------------------------|-----------|-------------------|
| 4096  + 500 								  | 00:16:416 | new base line  	  |
| 8192  + 500 								  | 00:16:272 |       -0,88%      |
| 16384 + 500  								  | 00:16:393 |       -0,14%      |
| 32768 + 500  								  | 00:16:016 |       -2,44%      |
| 65536 + 500  								  | 00:15:910 |       -3,08%      |
| 4096  + 1000 								  | 00:15:147 |       -7,73%      |
| 8192  + 1000 								  | 00:14:997 |       -8,64%      |
| 16384 + 1000 								  | 00:15:019 |       -8,51%      |
| 32768 + 1000 								  | 00:15:204 |       -7,38%      |
| 65536 + 1000 								  | 00:15:001 |       -8,62%      |
| 4096  + 2000 								  | 00:14:444 |      -12,01%      |
| 8192  + 2000 								  | 00:14:523 |      -11,53%      |
| 16384 + 2000 								  | 00:14:456 |      -11,94%      |
| 32768 + 2000 								  | 00:14:493 |      -11,71%      |
| 65536 + 2000 								  | 00:14:347 |      -12,60%      |
| 4096  + 4000  							  | 00:14:011 |      -14,65%      |
| 8192  + 4000  							  | 00:13:696 |      -16,57%      |
| 16384 + 4000  							  | 00:13:659 |      -16,79%      |
| **32768 + 4000**  						  |**00:13:616**|  **-17,06%**    |
| 65536 + 4000  							  | 00:13:862 |      -15,56%      |
| 4096  + 5000  							  | 00:14:084 |      -14,21%      |
| 8192  + 5000  							  | 00:13:848 |      -15,64%      |
| 16384 + 5000  							  | 00:13:825 |      -15,78%      |
| 32768 + 5000  							  | 00:14:133 |      -13,91%      |
| 65536 + 5000  							  | 00:13:906 |      -15,29%      |
| 4096  + 8000  							  | 00:14:107 |      -14,06%      |
| 8192  + 8000  							  | 00:14:133 |      -13,91%      |
| 16384 + 8000  							  | 00:14:148 |      -13,82%      |
| 32768 + 8000  							  | 00:14:064 |      -14,33%      |
| 65536 + 8000  							  | 00:14:213 |      -13,42%      |
| 4096  + 10000  							  | 00:14:805 |       -9,81%      |
| 8192  + 10000  							  | 00:14:855 |       -9,51%      |
| 16384 + 10000  							  | 00:14:991 |       -8,68%      |
| 32768 + 10000  							  | 00:14:906 |       -9,20%      |
| 65536 + 10000  							  | 00:14:901 |       -9,23%      |

Test result:
- First of all I noticed that I screwed up the time measurements in the test before because the file delete was inside the time tracking code. This is not a big adjustment but it changed the measured time slightly and that is why the whole default buffer sizes needs to be tested again with the different line count.
- This took a while to write down but now it is clear that generating 4000 lines in parallel and writing them to file with a file buffer size of 32768 has the best performance (these values will differ depending on the hardware).
- The problem with this result is that it is only an approximation to the correct combination because the lines have a random sizes from 3 to 106 bytes which will result in 12000 to 424000 bytes written to the file with a buffer of 32768. This is not really optimal but the best result which can be archieved with this version.


**It took 23:13:214 to generate the final measurements file with 1B rows.**