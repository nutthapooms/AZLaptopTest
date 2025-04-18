#!/bin/bash
echo 'start test-----------------------' >> build_times.txt
for i in {1..5}
do
  # Record the start time
  start=$(date +%s%N)

  # Execute the command
 npm run build

  # Record the end time
  end=$(date +%s%N)

  # Calculate the time difference
  time_diff=$((end-start))

  # Append the time difference to a file
  echo $time_diff >> build_times.txt
done
echo 'end test-----------------------' >> build_times.txt

