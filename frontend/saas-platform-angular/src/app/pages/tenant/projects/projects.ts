import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService, Project } from '../../../core/services/project';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class Projects implements OnInit {
  projects: Project[] = [];
  filteredProjects: Project[] = [];

  searchQuery = '';
  statusFilter = '';

  isCreateModalOpen = false;
  newProject = { name: '', description: '', startDate: '', endDate: '', priority: 'Medium' };

  constructor(private projectService: ProjectService) {}

  ngOnInit() {
    this.loadProjects();
  }

  loadProjects() {
    this.projectService.getProjects().subscribe({
      next: (data: Project[]) => {
        this.projects = data;
        this.applyFilters();
      }
    });
  }

  applyFilters() {
    this.filteredProjects = this.projects.filter(p => {
      const matchesSearch = p.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                            p.description.toLowerCase().includes(this.searchQuery.toLowerCase());
      
      const matchesStatus = this.statusFilter === '' || p.status === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  onSearch() {
    this.applyFilters();
  }

  onFilterChange() {
    this.applyFilters();
  }

  openCreateModal() {
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    this.newProject = { name: '', description: '', startDate: today, endDate: nextMonth, priority: 'Medium' };
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
  }

  saveProject() {
    if (!this.newProject.name) return;

    this.projectService.createProject(this.newProject).subscribe({
      next: () => {
        this.loadProjects();
        this.closeCreateModal();
      }
    });
  }

  deleteProject(id: string) {
    if (confirm('Are you sure you want to delete this project? This will also remove all associated tasks.')) {
      this.projectService.deleteProject(id).subscribe({
        next: (success: boolean) => {
          if (success) {
            this.loadProjects();
          }
        }
      });
    }
  }
}

