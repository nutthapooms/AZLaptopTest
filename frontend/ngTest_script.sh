#!/bin/bash
echo 'start test-----------------------' >> test_times.txt
for i in {1..5}
do
  # Record the start time
  start=$(date +%s%N)

  # Execute the command
 npm run test

  # Record the end time
  end=$(date +%s%N)

  # Calculate the time difference
  time_diff=$((end-start))

  # Append the time difference to a file
  echo $time_diff >> test_times.txt
done
echo 'end test-----------------------' >> test_times.txt

