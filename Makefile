.PHONY: test test-hosted lint clean docker-build reward

PYTHON ?= python3
DOCKER_IMAGE ?= fruitsalad-harness
DOCKER_TAG ?= latest

test:
	$(PYTHON) tests/run_tests.py

test-hosted: docker-build
	$(PYTHON) tests/run_tests.py --harness docker

lint:
	$(PYTHON) -m flake8 fs/ harness/ tests/ --max-line-length 120 --exclude __pycache__
	$(PYTHON) -m mypy fs/ --ignore-missing-imports

clean:
	find . -type d -name __pycache__ -exec rm -rf {} + 2>/dev/null || true
	find . -type f -name '*.pyc' -delete 2>/dev/null || true
	rm -f metrics/test_results.json

docker-build:
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) -f harness/hosted/Dockerfile .

reward:
	@$(PYTHON) -c "import json, pathlib; \
	r = pathlib.Path('metrics/test_results.json'); \
	d = json.loads(r.read_text()) if r.exists() else {}; \
	p = d.get('passing', 0); t = d.get('total', 0); \
	s = p/t if t else 0.0; \
	print(f'Reward: {p}/{t} ({s:.1%})')"
