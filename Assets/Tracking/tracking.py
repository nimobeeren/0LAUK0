#!/usr/bin/env python3
"""
Script written by Daniël Barenholz & Nimo Beeren
for the course 0LAUK0 at Eindhoven University of Technology.

Performs object tracking on a continuously updating image using OpenCV.

Tested using Python 3.7.2 and OpenCV 3.4.4
"""

import cv2
import os
import time
import sys


colors = {
	'boosting': (255, 0, 0),
	'MIL': (0, 255, 0),
	'KCF': (0, 0, 255),
	'TLD': (255, 255, 0),
	'MedianFlow': (255, 0, 255),
	'MOSSE': (0, 255, 255),
	'CSRT': (255, 255, 255)
}

def draw_bbox(bboxes, frame):
	"""Draws BBOX on screen."""
	if len(bboxes) == 0:
		return
	else:
		print_bboxes(bboxes, frame)
		for tracker in bboxes:
			bbox = bboxes[tracker]
			p1 = (int(bbox[0]), int(bbox[1]))
			p2 = (int(bbox[0] + bbox[2]), int(bbox[1] + bbox[3]))
			cv2.rectangle(frame, p1, p2, colors[tracker], 2, 1)


def print_bboxes(bboxes, frame):
	"""Prints a list of bboxes to stdout."""
	frameH, frameW, _ = frame.shape
	counter = 0
	for b in bboxes:
		if counter > 0:
			break  # only print the first bbox

		bbox = bboxes[b]
		if len(bbox) < 4:
			raise ValueError("Bounding box has invalid format")

		# Get bbox as proportion of frame size
		bbox2 = (
			bbox[0] / frameW,
			bbox[1] / frameH,
			bbox[2] / frameW,
			bbox[3] / frameH
		)

		print(bbox2)
		sys.stdout.flush()  # make sure the output is immediately sent out
		counter += 1


def read_image(file):
	frame = cv2.imread(file)

	# For some reason, when our drone camera outputs in PPM format, the image is flipped horizontally,
	# so we flip it back
	filename, extension = os.path.splitext(file)
	if extension.lower() == ".ppm":
		frame = cv2.flip(frame, 0)  # flip image horizontally

	return frame


if __name__ == '__main__':
	# Get path to image file
	if len(sys.argv) > 1:
		file = os.path.abspath(sys.argv[1])
	else:
		file = os.path.abspath("droneCam.ppm")

	# Read first frame from image
	frame = read_image(file)

	# Let user select a bounding box using the first frame
	bbox = cv2.selectROI(frame, False)

	# Create trackers
	# Running all trackers at the same time results in 2 FPS, so a selection has been made
	trackers = {}
	# trackers['boosting'] = cv2.TrackerBoosting_create()
	# trackers['MIL'] = cv2.TrackerMIL_create()
	trackers['KCF'] = cv2.TrackerKCF_create()
	# trackers['TLD'] = cv2.TrackerTLD_create()
	# trackers['MedianFlow'] = cv2.TrackerMedianFlow_create()
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
			frame = read_image(file)
			if frame is None:
				raise Exception('Failed to read image')
		except Exception as e:
			print(e)
			fails += 1
			continue
		
		# Assume frame was loaded correctly
		fails = 0

		# Run trackers
		bboxes = {}
		for t in trackers:
			success, bbox = trackers[t].update(frame)
			if success:
				bboxes[t] = bbox

		# Draw resulting bounding boxes on screen
		draw_bbox(bboxes, frame)

		# Display FPS on frame
		timer = cv2.getTickCount()
		fps = cv2.getTickFrequency() / (cv2.getTickCount() - timer);
		cv2.putText(frame, "FPS : " + str(int(fps)), (400,20), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (50,170,50), 2);

		# Draw names with respective colors
		y = 20
		for t in trackers:
			cv2.putText(frame, t, (0, y), cv2.FONT_HERSHEY_SIMPLEX, 0.75, colors[t], 2);
			y += 20
		
		# Display result
		cv2.imshow("Tracking", frame)

		# Wait indefinitely
		cv2.waitKey(1)
