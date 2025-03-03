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
- Intel Core i5-4200U - max. 2.3 GHz
- DDR3 - 8GB RAM - 1600 MHz
- Toshiba MQ01ABF050 - Read 100 MB/s Write 96 MB/s
Visual Studio 2019 (Version 16.11.44)
- .NET Framework 4.7.2
- .NET 5 (Core)

## Writing & reading a file
There are a couple of classes that could be used to read & write data to & from a file:
- _FileInfo.CreateText_
- _File.Create_
- _File.CreateText_
- _File.ReadAllText_
- _StreamWriter_
- _StreamReader_
- _BinaryWriter_
- _BinaryReader_
- _FileStream_
- _Memory mapped files_
- _P/invoke native methods_

After an initial test with reduced data the _FileStream_ with default settings was the clear winner. 
Because I wanted to understand why that was the case I took a deep dive in the documentation, code and performance tests for a couple of days.
The collected informations and results can be found [here](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/FileReadAndWritePerformanceTests.md). 

## Generating the measurements file
The logic to generate the rows for the measurements isn't too complicated but writing the file might take a lot of time. So before generating the final measurements file with 1B rows (~12GB) it would be best to improve the code a bit.

||Duration|File size in GB|MB/s|Improvement in %||
|----|----|----|----|----|----|
|StreamWriter.WriteLine(line as string) in .NET Framework (name length 7)	|11:03:084|10.720|16.17|base line|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/3b483cf764121015bca7112b00917f6bb85ae205/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|FileStream.Write(line as byte array)        								|12:01:301|16.750|23.22|42.19|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/b4297a488cf753ff386b65265ac879949835565c/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|FileStream.Write(block filled multiple lines)								|11:04:100|13.201|19.88|22.94|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/bd29691691d72728d57bf21dc85701c296bd425b/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Reduced array creation & copy when combining the line						|10:00:579|13.201|21.98|35.93|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/001b7d09ec64fa2b545f20d00a19b3eb690be3f5/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Temperature generation writes bytes directly to block						|03:17:209|11.595|58.79|263.57|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/6c6ce5dd1643036ba76aaa38970b371ed13e7e80/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Parallel execution															|03:06:660|13.195|70.69|337.17|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/11ca4ff1f15286995bb39a7700c93c52d4c27628/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Copy name with Marshal.Copy instead of Array.Copy							|02:49:013|13.195|78.07|382.81|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/e6cba3b8b72181f59cfb5feeef768ce541533255/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Replaced real random with iteration through the values						|02:40:766|13.033|81.07|401.36|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/1fc204e8373f380faac7089398a233f17f6c5fa8/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Refactored code (parallel line generation & asyn write)					|02:39:172|13.200|82.93|412.86|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/cf2aecdc0dbd90d8a94d291e99cb2650fa390202/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Allocate slices, store pointers & P/invoke CopyMemory						|02:35:141|13.200|85.08|426.16|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/5e74c4afe7b004d1896cb74a202f76a4f7bbb4a6/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|P/invoke CreateFile, WriteFile & CloseHandle								|02:26:106|13.200|90.34|458.69|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/adebb06dea65e0ca90efc6b950eb35d65a65d9d9/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Removed slices & write directly to block (added fixed [10k names of list](https://github.com/gunnarmorling/1brc/blob/main/data/weather_stations.csv))|02:52:751|15.157|87.74|442.61|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/d8f8124afe4b02e85a29bd325b0f073a493e536d/src/1BRC.ConsoleRunner/Generate/MeasurementsGenerator.cs)|
|Copied current code to .NET 5, changed to safe version & use spans			|02:46:481|14.882|89.39|452.81|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/ec8edc4674b064f3fed544ac6532a51413c5cb70/src/1BRC.Net5.ConsoleRunner/Generate/MeasurementsGenerator.cs)|

After a lot of tests and refactoring the final .NET Framework version with unsafe code and the .NET 5 version with safe code both reached about 90% of the possible write speed. With the spans instead of byte arrays the safe code performance similar to the unsafe code which normally would have been faster in comparison. 
Maybe with a bit of tweaking a couple more seconds could be shaved of the result but right now it is only 10 seconds off of the maximum write speed which might only be theoretical possible.
I might have to test this with a better device with more I/O speed in the future but for now it is enough. Also upgrading to newer versions of .NET will have a better performance.

## Reading the file
Now I can finally start with the actual challenge but I will do it with a step by step refactoring again.

||Duration|File size in GB|MB/s|Improvement in %||
|----|----|----|----|----|----|
|StreamWriter.ReadLine(line as string) in .NET Framework	|08:14:673|14.882|30.08|base line|[File](https://github.com/thoeltig/CodingChallenge.1BRC/blob/84c6189a7a9fcc46de65b1786343b7bcced70d3f/src/1BRC.ConsoleRunner/Read/MeasurementsReader.cs)|
|FileStream.ReadByte 										|08:04:929|14.882|30.69|2.03||
|Parse temperature to int instead of double					|		  |		 |		|||
|															|		  |		 |		|||