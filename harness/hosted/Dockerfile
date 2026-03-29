FROM python:3.12-slim

LABEL maintainer="fruitsalad"
LABEL description="Hosted execution harness for Fruit Salad programs"

# Create a non-root user for running programs
RUN groupadd -r fsuser && useradd -r -g fsuser -d /home/fsuser -m fsuser

# Copy the interpreter and harness
COPY fs/ /opt/fruitsalad/fs/
COPY harness/hosted/runner.py /opt/fruitsalad/runner.py

# Install any Python dependencies needed by the interpreter
COPY requirements.txt /tmp/requirements.txt
RUN pip install --no-cache-dir -r /tmp/requirements.txt && rm /tmp/requirements.txt

WORKDIR /opt/fruitsalad

# Default resource limits (can be overridden at runtime)
ENV FS_TIMEOUT=30
ENV FS_MEMORY_LIMIT_MB=256

# Run as non-root
USER fsuser

ENTRYPOINT ["python3", "/opt/fruitsalad/runner.py"]
