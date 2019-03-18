#!/usr/bin/env python3
"""
Script written by Daniël Barenholz & Nimo Beeren
for the course 0LAUK0 at Eindhoven University of Technology.

Performs object tracking on a continuously updating image using OpenCV.

Tested using Python 3.7.2 and OpenCV 3.4.4
"""

import time
import cv2
import sys


def draw_bbox(list_of_bbox):
	""" Draws BBOX on screen."""
	if len(list_of_bbox) == 0:
		return
	else:
		counter = 0 
		# MOSSE, KCF, CSRT
		# BLUE, LIGHTBLUE, GREEN
		cols = [(255, 0, 0), (255, 255, 0), (100, 100, 0)]
		for bbox in list_of_bbox:
			p1 = (int(bbox[0]), int(bbox[1]))
			p2 = (int(bbox[0] + bbox[2]), int(bbox[1] + bbox[3]))
			cv2.rectangle(frame, p1, p2, cols[counter], 2, 1)
			counter = counter + 1
		return


if __name__ == '__main__':
	# Print python and opencv version
	print("Using Python version: ", sys.version.split('|')[0])
	print("Using OpenCV version: ", cv2.__version__)

	# Path to image file
	if len(sys.argv) > 1:
		file = sys.argv[1]
	else:
		file = "/home/nimo/Share/Documents/Unity Projects/0LAUK0/scrot.ppm"

	# Read first frame from image
	frame = cv2.imread(file)

	# Let user select a bounding box using the first frame
	bbox = cv2.selectROI(frame, False)
	print("Selected bbox: ", bbox)

	# Create trackers
	# Running all trackers at the same time results in 2 FPS, so a selection has been made
	trackers = {}
	# trackers['boosting'] = cv2.TrackerBoosting_create()
	# trackers['MIL'] = cv2.TrackerMIL_create()
	# trackers['KCF'] = cv2.TrackerKCF_create()
	# trackers['TLD'] = cv2.TrackerTLD_create()
	trackers['MedianFlow'] = cv2.TrackerMedianFlow_create()
	trackers['MOSSE'] = cv2.TrackerMOSSE_create()
	# trackers['CSRT'] = cv2.TrackerCSRT_create()

	# Initialize trackers using specified bounding box
	for t in trackers:
		success = trackers[t].init(frame, bbox)
		if not success:
			print("Cannot initialize " + t + " tracker")
			sys.exit()

	# Keep refreshing the image and run trackers on it
	fails = 0
	max_fails = 20
	while True:
		# Exit when image read fails too many times in a row
		if fails >= max_fails:
			print("Failed to read image " + str(fails) + " times in a row, exiting")
			sys.exit()

		# Try to read image
		try:
			frame = cv2.imread(file)
			if frame is None:
				raise Exception('Failed to read image')
		except Exception as e:
			print(e)
			fails += 1
			continue
		
		# Assume frame was loaded correctly
		fails = 0

		# Run trackers
		bboxes = []
		for t in trackers:
			success, bbox = trackers[t].update(frame)
			print(t + ': ' + str(success) + ' | ', end='')
			if success:
				bboxes.append(bbox)
		print('\r', end='')

		# Draw resulting bounding boxes on screen
		draw_bbox(bboxes)

		# Display FPS on frame
		timer = cv2.getTickCount()
		fps = cv2.getTickFrequency() / (cv2.getTickCount() - timer);
		cv2.putText(frame, "FPS : " + str(int(fps)), (400,20), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (50,170,50), 2);

		# Draw names with respective colors
		cv2.putText(frame, "MOSSE ", (0,20), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 0, 0),2);
		cv2.putText(frame, "MedianFlow ", (100,20), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 255, 0),2);
		
		# Display result
		cv2.imshow("Tracking", frame)

		# Exit when the escape key is pressed
		k = cv2.waitKey(1) & 0xff
		if k == 27 : break
